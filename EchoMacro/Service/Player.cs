using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using static EchoMacro.Library.VirtualKey;

namespace EchoMacro.Service
{
    public class Player : INotifyPropertyChanged
    {
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

        private int globalDelay = 0;
        private readonly InputSimulator _simulator;
        private readonly Dictionary<string, VirtualKeyCode> _virtualKeyMap = GetVirtualKeyMap();

        public Player()
        {
            _simulator = new InputSimulator();
        }

        public async Task PlayActions(List<RecordedAction> actions, int delay = 0, bool repeat = false)
        {
            globalDelay = delay;
            if (actions == null || actions.Count == 0)
            {
                Console.WriteLine("No Action.");
                return;
            }

            do
            {
                //Take the first action as the base time.
                double baseTime = actions[0].Timestamp;
                double previousTime = baseTime;
                IsPlaying = true;

                foreach (var action in actions)
                {
                    double waitTime = action.Timestamp - previousTime;
                    if (waitTime > 0)
                        await Task.Delay((int)waitTime);

                    if (delay > 0)
                        await Task.Delay(delay);

                    if (!IsPlaying)
                        return;

                    ExecuteAction(action);

                    previousTime = action.Timestamp;
                }

            } while (repeat);
            IsPlaying = false;
        }
        public void StopPlayback() => IsPlaying = false;

        private void ExecuteAction(RecordedAction action)
        {
            switch (action.Type)
            {
                case InputType.MouseClick:
                    MouseClickHandler(action);
                    break;
                case InputType.KeyPress:
                    KeyPressHandler(action);
                    break;
                default:
                    break;
            }
        }
        private void MouseClickHandler(RecordedAction act)
        {
            //MoveMouseSmoothly(act.X, act.Y);
            MoveMouseSmoothlyWithErrorCompensation(act.X, act.Y);

            Thread.Sleep(5);
            if (act.IsRightClick)
            {
                _simulator.Mouse.RightButtonClick();
            }
            else
            {
                _simulator.Mouse.LeftButtonClick();
            }
        }
        private void KeyPressHandler(RecordedAction act)
        {
            if (Enum.TryParse(act.Key, out VirtualKeyCode keyCode) || TryGetKeyCodeParse(act.Key, out keyCode))
            {
                if(globalDelay == 0)
                {
                    _simulator.Keyboard.KeyPress(keyCode);
                }
                else
                {
                    Random rand = new Random();
                    _simulator.Keyboard.KeyDown(keyCode); //Simulate human-like performance
                    Thread.Sleep(rand.Next(50, 80));
                    _simulator.Keyboard.KeyUp(keyCode);
                }
            }
        }

        private bool TryGetKeyCodeParse(string str, out VirtualKeyCode keyCode)
        {
            keyCode = VirtualKeyCode.NONAME;
            if (!_virtualKeyMap.TryGetValue(str, out keyCode))
                return false;

            return true;
        }
        private async Task MoveMouseSmoothly(int targetX, int targetY)
        {
            int startX = Cursor.Position.X;
            int startY = Cursor.Position.Y;

            //Add steps to make the movement smoother.
            int steps = globalDelay == 0 ? 80 : 150;
            Random rand = new Random();

            for (int i = 1; i <= steps; i++)
            {
                double t = (double)i / steps;
                t = t * t * (3 - 2 * t); //Ease-in-Ease-out

                int newX = (int)(startX + t * (targetX - startX) + rand.Next(-3, 4)); //Simulate human-like performance
                int newY = (int)(startY + t * (targetY - startY) + rand.Next(-3, 4));

                Cursor.Position = new System.Drawing.Point(newX, newY);
                int minDelay = globalDelay == 0 ? 0 : 1;
                int maxDelay = globalDelay == 0 ? 2 : globalDelay;
                Thread.Sleep(rand.Next(minDelay, maxDelay)); //Add random delay
            }

            Cursor.Position = new System.Drawing.Point(targetX, targetY); //Final position.

            //Pending Refactor
            if (globalDelay != 0)
                Thread.Sleep(rand.Next(5, 20)); //Add random delay

        }

        /// <summary>Simulates human-like mouse movement with potential error and correction.</summary>
        private void MoveMouseSmoothlyWithErrorCompensation(int targetX, int targetY)
        {
            Random rand = new Random();

            //Error rate (e.g., 15% probability)
            bool simulateMistake = rand.NextDouble() < 0.15;
            int actualX = targetX;
            int actualY = targetY;

            if (simulateMistake)
            {
                actualX += rand.Next(-20, 21); // Intentionally offset to the wrong position
                actualY += rand.Next(-20, 21);
                MoveMouseSmoothlyCurve(actualX, actualY, globalDelay);
                Thread.Sleep(rand.Next(80, 150)); // Pause before correcting
            }

            MoveMouseSmoothlyCurve(targetX, targetY, globalDelay); //Real target position
            Thread.Sleep(rand.Next(10, 30));
        }

        private void MoveMouseSmoothlyCurve(int targetX, int targetY, int globalDelay)
        {
            int startX = Cursor.Position.X;
            int startY = Cursor.Position.Y;
            int steps = globalDelay == 0 ? 80 : 150;
            Random rand = new Random();

            // Bezier Curve
            Point p0 = new(startX, startY);
            Point p2 = new(targetX, targetY);
            Point p1 = new(
                (p0.X + p2.X) / 2 + rand.Next(-100, 100),
                (p0.Y + p2.Y) / 2 + rand.Next(-100, 100)
            );

            for (double t = 0; t <= 1.0; t += 1.0 / steps)
            {
                double oneMinusT = 1 - t;

                // Quadratic Bezier Curve
                int newX = (int)(oneMinusT * oneMinusT * p0.X + 2 * oneMinusT * t * p1.X + t * t * p2.X);
                int newY = (int)(oneMinusT * oneMinusT * p0.Y + 2 * oneMinusT * t * p1.Y + t * t * p2.Y);

                Cursor.Position = new Point(newX, newY);
                int minDelay = globalDelay == 0 ? globalDelay : 1;
                int maxDelay = globalDelay == 0 ? 3 : globalDelay;
                Thread.Sleep(rand.Next(minDelay, maxDelay));
            }

            Cursor.Position = new Point(targetX, targetY);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
    }
}
