using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using EchoMacro.Library;

namespace EchoMacro.Service
{
    public class Player : INotifyPropertyChanged
    {
        /// <summary>Minimum pause between two repeat iterations, so a zero-wait macro cannot spin flat out.</summary>
        private const int MinRepeatGapMs = 10;
        /// <summary>How long to wait for the user to let go of Ctrl/Alt/Shift/Win before playing.</summary>
        private const int ModifierReleaseTimeoutMs = 2000;
        /// <summary>Within this many pixels the cursor is considered to be on target already.</summary>
        private const int OnTargetDistance = 3;
        /// <summary>A cursor move is never squeezed below this, even when the recording leaves less time for it.</summary>
        private const double MinMoveMs = 60;
        /// <summary>Duration of the short corrective move after a simulated miss.</summary>
        private const double CorrectionMoveMs = 120;
        /// <summary>
        /// The user moving the real mouse for this long, without a break, stops playback.
        /// The wait keeps a bumped mouse from interrupting. 0 or less turns the feature off.
        /// </summary>
        public double AutoStopSeconds { get; set; } = 2;
        /// <summary>A pause longer than this ends a stretch of user movement; a bumped mouse never adds up to a takeover.</summary>
        private const int UserMoveBreakMs = 300;
        private const int UserMousePollMs = 50;
        /// <summary>Allowance for the system rounding a position the player set.</summary>
        private const int CursorTolerance = 2;

        private static readonly ushort[] ModifierKeys =
        {
            NativeInput.VK_SHIFT, NativeInput.VK_CONTROL, NativeInput.VK_MENU, NativeInput.VK_LWIN, NativeInput.VK_RWIN
        };

        private bool _isPlaying = false;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                OnPropertyChanged();
            }
        }

        private CancellationTokenSource? _cts;

        //State of the running playback. Only touched by the playback thread.
        private readonly HashSet<ushort> _heldKeys = new HashSet<ushort>();
        private readonly HashSet<MouseButtonKind> _heldButtons = new HashSet<MouseButtonKind>();
        private readonly Stopwatch _clock = new Stopwatch();

        //Where the player last put the cursor; a cursor anywhere else was moved by the user.
        private readonly object _cursorLock = new object();
        private Point _lastSetCursor;

        /// <summary>
        /// Plays the actions on a background thread so the UI stays responsive.
        /// Completes when playback finishes or <see cref="StopPlayback"/> is called.
        /// </summary>
        public async Task PlayActions(List<RecordedAction> actions, int delay = 0, bool repeat = false)
        {
            if (IsPlaying || actions == null)
                return;

            List<RecordedAction> playable = actions.Where(a => a != null).ToList();
            if (playable.Count == 0)
                return;

            int actionDelay = Math.Max(0, delay);
            var cts = new CancellationTokenSource();
            _cts = cts;
            IsPlaying = true;

            _lastSetCursor = Cursor.Position;
            Task watcher = WatchForUserMouse(cts);

            try
            {
                await Task.Run(() => PlayLoop(playable, actionDelay, repeat, cts.Token));
            }
            catch (OperationCanceledException)
            {
                //Stopped by the user.
            }
            finally
            {
                cts.Cancel(); //Ends the watcher when playback finished on its own.
                await watcher;
                _cts = null;
                cts.Dispose();
                IsPlaying = false;
            }
        }
        public void StopPlayback() => _cts?.Cancel();

        /// <summary>
        /// Stops playback once the user has been moving the real mouse for <see cref="AutoStopSeconds"/> without
        /// a break. It tells the user apart from the macro by position: the macro only ever puts the cursor
        /// where <see cref="SetCursor"/> recorded it.
        /// </summary>
        private async Task WatchForUserMouse(CancellationTokenSource cts)
        {
            double takeoverMs = AutoStopSeconds * 1000;
            if (!(takeoverMs > 0))
                return;

            CancellationToken token = cts.Token;
            var clock = Stopwatch.StartNew();
            Point lastSeen = Cursor.Position;
            long stretchStart = 0;
            long lastUserMove = -UserMoveBreakMs - 1;

            try
            {
                while (true)
                {
                    await Task.Delay(UserMousePollMs, token).ConfigureAwait(false);

                    bool movedByUser;
                    lock (_cursorLock)
                    {
                        Point position = Cursor.Position;
                        bool placedByPlayer = Math.Abs(position.X - _lastSetCursor.X) <= CursorTolerance
                                           && Math.Abs(position.Y - _lastSetCursor.Y) <= CursorTolerance;
                        movedByUser = !placedByPlayer && position != lastSeen;
                        lastSeen = position;
                    }
                    if (!movedByUser)
                        continue;

                    long now = clock.ElapsedMilliseconds;
                    if (now - lastUserMove > UserMoveBreakMs)
                        stretchStart = now;
                    lastUserMove = now;

                    if (now - stretchStart >= takeoverMs)
                    {
                        cts.Cancel();
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //Playback ended.
            }
        }

        private void SetCursor(int x, int y)
        {
            lock (_cursorLock)
            {
                Cursor.Position = new Point(x, y);
                _lastSetCursor = new Point(x, y);
            }
        }

        private void PlayLoop(List<RecordedAction> actions, int delay, bool repeat, CancellationToken token)
        {
            bool humanLike = delay > 0;

            //The default Windows timer ticks every ~15 ms, far too coarse for smooth cursor movement
            //and for reproducing the timing of clicks and key combinations.
            NativeInput.BeginHighResolutionTimer();
            try
            {
                //Playback is usually started with a hotkey; modifiers still held would combine with the macro's keys.
                WaitForModifierRelease(token);
                _clock.Restart();

                do
                {
                    double previousTimestamp = actions[0].Timestamp;
                    double due = Now;

                    for (int i = 0; i < actions.Count; i++)
                    {
                        RecordedAction action = actions[i];

                        //Each action keeps its recorded distance to the previous one.
                        due += Math.Max(0, ToFinite(action.Timestamp - previousTimestamp)) + delay;
                        previousTimestamp = action.Timestamp;

                        //The cursor travels during the gap before the action, so the action itself is on time.
                        //Nothing precedes the first action, so it may take whatever time a natural move needs.
                        if (NeedsApproach(action))
                            ApproachTarget(action.X, action.Y, due, humanLike, unhurried: i == 0, token);

                        WaitUntil(due, token);
                        ExecuteAction(action, humanLike);

                        //Running late pushes the rest of the timeline back instead of squeezing the following
                        //gaps: catching up would turn a click into a zero-length press.
                        if (Now > due)
                            due = Now;
                    }

                    //A recording can end with something still held; never carry that into the next round.
                    ReleaseHeldInputs();

                    if (repeat)
                        Wait(MinRepeatGapMs, token);

                } while (repeat);
            }
            finally
            {
                ReleaseHeldInputs();
                NativeInput.EndHighResolutionTimer();
            }
        }

        private double Now => _clock.Elapsed.TotalMilliseconds;

        private void WaitForModifierRelease(CancellationToken token)
        {
            var timeout = Stopwatch.StartNew();
            while (timeout.ElapsedMilliseconds < ModifierReleaseTimeoutMs
                   && ModifierKeys.Any(NativeInput.IsKeyDown))
            {
                Wait(10, token);
            }
        }

        /// <summary>Sleeps for the given time, returning early (by throwing) when playback is stopped.</summary>
        private static void Wait(double milliseconds, CancellationToken token)
        {
            if (milliseconds >= 1)
                token.WaitHandle.WaitOne((int)Math.Min(milliseconds, int.MaxValue));

            token.ThrowIfCancellationRequested();
        }

        private void WaitUntil(double dueTime, CancellationToken token) => Wait(dueTime - Now, token);

        private static double ToFinite(double value) => double.IsFinite(value) ? value : 0;

        /// <summary>Recorded drag moves are replayed as they are; every other positioned action gets a generated approach.</summary>
        private static bool NeedsApproach(RecordedAction action) =>
            action.Type is InputType.MouseClick or InputType.MouseDown or InputType.MouseUp or InputType.MouseWheel;

        private void ExecuteAction(RecordedAction action, bool humanLike)
        {
            switch (action.Type)
            {
                case InputType.MouseClick:
                    //Legacy single-action click.
                    SetCursor(action.X, action.Y);
                    MouseButtonKind clickButton = action.IsRightClick ? MouseButtonKind.Right : MouseButtonKind.Left;
                    PressButton(clickButton);
                    ReleaseButton(clickButton);
                    break;
                case InputType.MouseDown:
                    SetCursor(action.X, action.Y);
                    PressButton(action.Button);
                    break;
                case InputType.MouseUp:
                    SetCursor(action.X, action.Y);
                    ReleaseButton(action.Button);
                    break;
                case InputType.MouseMove:
                    SetCursor(action.X, action.Y);
                    break;
                case InputType.MouseWheel:
                    SetCursor(action.X, action.Y);
                    NativeInput.MouseWheel(action.WheelDelta);
                    break;
                case InputType.KeyPress:
                    KeyPressHandler(action, humanLike);
                    break;
                case InputType.KeyDown:
                    if (TryResolveKey(action, out ushort downKey))
                    {
                        NativeInput.KeyDown(downKey);
                        _heldKeys.Add(downKey);
                    }
                    break;
                case InputType.KeyUp:
                    if (TryResolveKey(action, out ushort upKey))
                    {
                        NativeInput.KeyUp(upKey);
                        _heldKeys.Remove(upKey);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>Legacy single-action key press.</summary>
        private void KeyPressHandler(RecordedAction act, bool humanLike)
        {
            if (!TryResolveKey(act, out ushort keyCode))
                return;

            NativeInput.KeyDown(keyCode);
            if (humanLike)
                Thread.Sleep(Random.Shared.Next(50, 80)); //Not cancellable: the key must always be released.
            NativeInput.KeyUp(keyCode);
        }

        private bool TryResolveKey(RecordedAction act, out ushort keyCode)
        {
            //Current files store the virtual-key code itself.
            if (act.KeyCode > 0 && act.KeyCode < 0xFF)
            {
                keyCode = (ushort)act.KeyCode;
                return true;
            }

            //Legacy files only have the key name, as written by System.Windows.Forms.Keys.ToString().
            //The numeric value of a Keys member is its virtual-key code.
            keyCode = 0;
            if (string.IsNullOrEmpty(act.Key))
                return false;

            string name = act.Key.Equals("ESC", StringComparison.OrdinalIgnoreCase) ? nameof(Keys.Escape) : act.Key;
            if (!Enum.TryParse(name, ignoreCase: true, out Keys key))
                return false;

            int value = (int)key;
            if (value <= 0 || value >= 0xFF)
                return false;

            keyCode = (ushort)value;
            return true;
        }

        private void PressButton(MouseButtonKind button)
        {
            NativeInput.MouseButton(button, down: true);
            _heldButtons.Add(button);
        }
        private void ReleaseButton(MouseButtonKind button)
        {
            NativeInput.MouseButton(button, down: false);
            _heldButtons.Remove(button);
        }

        /// <summary>Lets go of everything the macro still holds, so stopping mid-macro never leaves a key or button stuck.</summary>
        private void ReleaseHeldInputs()
        {
            foreach (ushort key in _heldKeys)
                NativeInput.KeyUp(key);
            _heldKeys.Clear();

            foreach (MouseButtonKind button in _heldButtons)
                NativeInput.MouseButton(button, down: false);
            _heldButtons.Clear();
        }

        /// <summary>
        /// Brings the cursor to the target so that it arrives shortly before <paramref name="due"/>.
        /// The move is fitted into the time the recording leaves; with time to spare it starts later rather
        /// than moving slower, and occasionally misses first and corrects, like a person would.
        /// </summary>
        private void ApproachTarget(int targetX, int targetY, double due, bool humanLike, bool unhurried, CancellationToken token)
        {
            Random rand = Random.Shared;
            double distance = DistanceTo(targetX, targetY);

            //Already there (second half of a click, a double click): leave the cursor alone.
            if (distance <= OnTargetDistance)
                return;

            //Farther targets take longer to reach, within limits.
            double natural = Math.Clamp(120 + distance * 0.35, 120, 450) * (humanLike ? 1.5 : 1.0);
            double settle = rand.Next(10, 30);
            double available = due - settle - Now;
            bool dragging = _heldButtons.Count > 0;

            //Error rate (e.g., 15% probability). Never while dragging: a detour would change what the drag does.
            double pause = rand.Next(80, 150);
            double withMistake = natural + pause + CorrectionMoveMs;
            bool simulateMistake = !dragging && distance > 50 && available >= withMistake && rand.NextDouble() < 0.15;

            if (simulateMistake)
            {
                WaitUntil(due - settle - withMistake, token);

                // Intentionally offset to the wrong position
                int wrongX = targetX + rand.Next(-20, 21);
                int wrongY = targetY + rand.Next(-20, 21);
                MoveAlongCurve(wrongX, wrongY, natural, token);
                Wait(pause, token); // Pause before correcting
                MoveAlongCurve(targetX, targetY, CorrectionMoveMs, token);
                return;
            }

            double duration = unhurried ? natural : Math.Max(Math.Min(natural, available), MinMoveMs);
            WaitUntil(due - settle - duration, token);
            MoveAlongCurve(targetX, targetY, duration, token);
        }

        private static double DistanceTo(int targetX, int targetY)
        {
            Point current = Cursor.Position;
            double dx = targetX - current.X;
            double dy = targetY - current.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Moves the cursor along a quadratic Bezier curve in the given time, accelerating out of the start
        /// and slowing into the target. A move made with a button held goes in a straight line.
        /// </summary>
        private void MoveAlongCurve(int targetX, int targetY, double durationMs, CancellationToken token)
        {
            Random rand = Random.Shared;
            double distance = DistanceTo(targetX, targetY);
            int bend = _heldButtons.Count > 0 ? 0 : (int)Math.Clamp(distance / 4, 0, 80);

            // Bezier Curve
            Point p0 = Cursor.Position;
            Point p2 = new(targetX, targetY);
            Point p1 = new(
                (p0.X + p2.X) / 2 + rand.Next(-bend, bend + 1),
                (p0.Y + p2.Y) / 2 + rand.Next(-bend, bend + 1)
            );

            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                double progress = stopwatch.Elapsed.TotalMilliseconds / durationMs;
                if (progress >= 1.0)
                    break;

                double t = progress * progress * (3 - 2 * progress); //Ease-in-Ease-out
                double oneMinusT = 1 - t;

                // Quadratic Bezier Curve
                int newX = (int)Math.Round(oneMinusT * oneMinusT * p0.X + 2 * oneMinusT * t * p1.X + t * t * p2.X);
                int newY = (int)Math.Round(oneMinusT * oneMinusT * p0.Y + 2 * oneMinusT * t * p1.Y + t * t * p2.Y);

                SetCursor(newX, newY);
                Wait(1, token);
            }

            SetCursor(p2.X, p2.Y);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
    }
}
