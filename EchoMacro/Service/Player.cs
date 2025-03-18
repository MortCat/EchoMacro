using Gma.System.MouseKeyHook;
using System.ComponentModel;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using static EchoMacro.Library.VirtualKey;

namespace EchoMacro.Service;
public class Player : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

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
    private void OnPropertyChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));

    private readonly InputSimulator _simulator;
    private readonly Dictionary<string, VirtualKeyCode> _virtualKeyMap = GetVirtualKeyMap();

    public Player()
    {
        _simulator = new InputSimulator();
    }

    public async Task PlayActions(List<RecordedAction> actions, int delay = 0, bool repeat = false)
    {
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
        MoveMouseSmoothly(act.X, act.Y);

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
            _simulator.Keyboard.KeyPress(keyCode);
        }
    }

    private bool TryGetKeyCodeParse(string str, out VirtualKeyCode keyCode)
    {
        keyCode = VirtualKeyCode.NONAME;
        if (!_virtualKeyMap.TryGetValue(str, out keyCode))
            return false;

        return true;
    }
    private void MoveMouseSmoothly(int targetX, int targetY)
    {
        int startX = Cursor.Position.X;
        int startY = Cursor.Position.Y;

        //Add steps to make the movement smoother.
        int steps = 150;
        Random rand = new Random();

        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            t = t * t * (3 - 2 * t); //Ease-in-Ease-out

            int newX = (int)(startX + t * (targetX - startX) + rand.Next(-2, 2)); //Simulate human-like performance
            int newY = (int)(startY + t * (targetY - startY) + rand.Next(-2, 2));

            Cursor.Position = new System.Drawing.Point(newX, newY);
            Thread.Sleep(rand.Next(1, 5)); //Add random delay
        }

        Cursor.Position = new System.Drawing.Point(targetX, targetY); //Final position.
    }

}
