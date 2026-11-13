using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace EchoMacro.Service
{
    public class Recorder : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>True while the current recording exists only in memory.</summary>
        public bool HasUnsavedChanges { get; private set; }
        public void MarkSaved() => HasUnsavedChanges = false;

        /// <summary>True when there is something to play: a finished recording or a loaded file.</summary>
        public bool HasActions => _recordedActions.Count > 0;
        private IKeyboardMouseEvents? _globalHook;
        private List<RecordedAction> _recordedActions = new List<RecordedAction>();
        private readonly Stopwatch _stopwatch = new Stopwatch();

        //Inputs that went down during this recording. Used to drop keyboard auto-repeat
        //and releases whose press was never recorded.
        private readonly HashSet<Keys> _heldKeys = new HashSet<Keys>();
        private readonly HashSet<MouseButtons> _heldButtons = new HashSet<MouseButtons>();

        /// <summary>Drag moves closer together than this are dropped; 8 ms keeps about 125 points per second.</summary>
        private const double MinMoveIntervalMs = 8;
        private double _lastMoveTimestamp;

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool _isRecording;
        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                _isRecording = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRecording)));

        public void StartRecording()
        {
            if (IsRecording) return;
            Init();
        }
        private void Init()
        {
            _recordedActions.Clear();
            HasUnsavedChanges = false;
            _heldKeys.Clear();
            _heldButtons.Clear();
            _lastMoveTimestamp = 0;
            _stopwatch.Restart();
            IsRecording = true;
            Name = "Recorded" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

            _globalHook = Hook.GlobalEvents();
            _globalHook.MouseDown += OnMouseDown;
            _globalHook.MouseUp += OnMouseUp;
            _globalHook.MouseMove += OnMouseMove;
            _globalHook.MouseWheel += OnMouseWheel;
            _globalHook.KeyDown += OnKeyDown;
            _globalHook.KeyUp += OnKeyUp;
        }

        public void StopRecording()
        {
            if (!IsRecording) return;

            if (_globalHook != null)
            {
                _globalHook.MouseDown -= OnMouseDown;
                _globalHook.MouseUp -= OnMouseUp;
                _globalHook.MouseMove -= OnMouseMove;
                _globalHook.MouseWheel -= OnMouseWheel;
                _globalHook.KeyDown -= OnKeyDown;
                _globalHook.KeyUp -= OnKeyUp;
                _globalHook.Dispose();
                _globalHook = null;
            }
            _stopwatch.Stop();

            RemoveUnreleasedKeyPresses();
            HasUnsavedChanges = _recordedActions.Count > 0;
            IsRecording = false;
        }

        /// <summary>
        /// Drops key presses that were never released before recording stopped. In practice these are the
        /// keys of the stop hotkey itself, which would otherwise be replayed at the end of the macro.
        /// </summary>
        private void RemoveUnreleasedKeyPresses()
        {
            foreach (Keys key in _heldKeys)
            {
                int code = (int)key;
                int lastRelease = _recordedActions.FindLastIndex(a => a.Type == InputType.KeyUp && a.KeyCode == code);

                for (int i = _recordedActions.Count - 1; i > lastRelease; i--)
                {
                    if (_recordedActions[i].Type == InputType.KeyDown && _recordedActions[i].KeyCode == code)
                        _recordedActions.RemoveAt(i);
                }
            }
            _heldKeys.Clear();
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (!IsRecording) return;
            if (!TryMapButton(e.Button, out MouseButtonKind button)) return;

            //Clicks on this app (e.g. the Stop button, dragging the toolbar) are not part of the macro.
            if (IsOwnWindowAt(e.X, e.Y)) return;

            _heldButtons.Add(e.Button);
            _recordedActions.Add(new RecordedAction
            {
                Type = InputType.MouseDown,
                Timestamp = _stopwatch.Elapsed.TotalMilliseconds,
                X = e.X,
                Y = e.Y,
                Button = button
            });
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            if (!IsRecording) return;
            if (!TryMapButton(e.Button, out MouseButtonKind button)) return;

            //A release without a recorded press: the press was before recording started or on this app.
            if (!_heldButtons.Remove(e.Button)) return;

            _recordedActions.Add(new RecordedAction
            {
                Type = InputType.MouseUp,
                Timestamp = _stopwatch.Elapsed.TotalMilliseconds,
                X = e.X,
                Y = e.Y,
                Button = button
            });
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            //Only the path of a drag matters; free movement is regenerated on playback.
            if (!IsRecording || _heldButtons.Count == 0) return;

            double now = _stopwatch.Elapsed.TotalMilliseconds;
            if (now - _lastMoveTimestamp < MinMoveIntervalMs) return;
            _lastMoveTimestamp = now;

            _recordedActions.Add(new RecordedAction
            {
                Type = InputType.MouseMove,
                Timestamp = now,
                X = e.X,
                Y = e.Y
            });
        }

        private void OnMouseWheel(object? sender, MouseEventArgs e)
        {
            if (!IsRecording || e.Delta == 0) return;
            if (IsOwnWindowAt(e.X, e.Y)) return;

            _recordedActions.Add(new RecordedAction
            {
                Type = InputType.MouseWheel,
                Timestamp = _stopwatch.Elapsed.TotalMilliseconds,
                X = e.X,
                Y = e.Y,
                WheelDelta = e.Delta
            });
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!IsRecording) return;

            //Holding a key makes Windows resend KeyDown. Those repeats are recorded too: a simulated key that is
            //merely held does not auto-repeat, so without them holding Backspace would delete a single character.
            _heldKeys.Add(e.KeyCode);

            AddKeyAction(InputType.KeyDown, e.KeyCode);
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            if (!IsRecording) return;
            if (!_heldKeys.Remove(e.KeyCode)) return;

            AddKeyAction(InputType.KeyUp, e.KeyCode);
        }

        private void AddKeyAction(InputType type, Keys key)
        {
            _recordedActions.Add(new RecordedAction
            {
                Type = type,
                Timestamp = _stopwatch.Elapsed.TotalMilliseconds,
                Key = key.ToString(),
                KeyCode = (int)key
            });
        }

        private static bool TryMapButton(MouseButtons source, out MouseButtonKind button)
        {
            switch (source)
            {
                case MouseButtons.Left: button = MouseButtonKind.Left; return true;
                case MouseButtons.Right: button = MouseButtonKind.Right; return true;
                case MouseButtons.Middle: button = MouseButtonKind.Middle; return true;
                case MouseButtons.XButton1: button = MouseButtonKind.XButton1; return true;
                case MouseButtons.XButton2: button = MouseButtonKind.XButton2; return true;
                default: button = MouseButtonKind.Left; return false;
            }
        }

        private static bool IsOwnWindowAt(int x, int y)
        {
            IntPtr window = WindowFromPoint(new POINT { X = x, Y = y });
            if (window == IntPtr.Zero) return false;

            GetWindowThreadProcessId(window, out uint processId);
            return processId == (uint)Environment.ProcessId;
        }

        public List<RecordedAction> GetRecordedActions() => new List<RecordedAction>(_recordedActions);
        public void SetRecorder(string recorderName, List<RecordedAction> records)
        {
            Name = recorderName;
            _recordedActions = records;
            HasUnsavedChanges = false;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    }
}
