using Gma.System.MouseKeyHook;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using static EchoMacro.Library.VirtualKey;

public class Player
{
    private InputSimulator simulator;
    private bool isPlaying;
    private CancellationTokenSource cts;
    private IKeyboardMouseEvents globalHook;
    private readonly Dictionary<string, VirtualKeyCode> virtualKeyMap = GetVirtualKeyMap();

    public Player()
    {
        simulator = new InputSimulator();
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

            foreach (var action in actions)
            {
                double waitTime = action.Timestamp - previousTime;
                if (waitTime > 0)
                    await Task.Delay((int)waitTime);

                if (delay > 0)
                    await Task.Delay(delay);

                ExecuteAction(action);

                previousTime = action.Timestamp;
            }

        } while (repeat);
    }

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
            simulator.Mouse.RightButtonClick();
        }
        else
        {
            simulator.Mouse.LeftButtonClick();
        }
    }
    private void KeyPressHandler(RecordedAction act)
    {
        if (Enum.TryParse(act.Key, out VirtualKeyCode keyCode) || TryGetKeyCodeParse(act.Key, out keyCode))
        {
            simulator.Keyboard.KeyPress(keyCode);
        }
    }

    private bool TryGetKeyCodeParse(string str, out VirtualKeyCode keyCode)
    {
        keyCode = VirtualKeyCode.NONAME;
        if (!virtualKeyMap.TryGetValue(str, out keyCode))
            return false;

        return true;
    }
    private void MoveMouseSmoothly(int targetX, int targetY)
    {
        int startX = Cursor.Position.X;
        int startY = Cursor.Position.Y;

        //Add steps to make the movement smoother.
        int steps = 50;
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
