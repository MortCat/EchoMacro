using Gma.System.MouseKeyHook;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

public class Player
{
    private InputSimulator _simulator;
    private bool isPlaying;
    private CancellationTokenSource cts;
    private IKeyboardMouseEvents globalHook;
    private readonly Dictionary<string, VirtualKeyCode> virtualKeyMap = GenerateVirtualKeyMap();

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
            double baseTime = actions[0].Timestamp; // 取第一個動作作為基準時間
            double previousTime = baseTime;

            foreach (var action in actions)
            {
                // 計算兩個動作之間的時間間隔
                double waitTime = action.Timestamp - previousTime;
                if (waitTime > 0)
                    await Task.Delay((int)waitTime);

                // 追加全局延遲
                if (delay > 0)
                    await Task.Delay(delay);

                // 執行鍵鼠動作
                ExecuteAction(action);

                // 更新前一個時間戳
                previousTime = action.Timestamp;
            }

        } while (repeat);
    }

    private void ExecuteAction(RecordedAction action)
    {
        if (action.Type == InputType.MouseClick)
        {
            MoveMouseSmoothly(action.X, action.Y);

            Thread.Sleep(5);
            if (action.IsRightClick)
            {
                _simulator.Mouse.RightButtonClick();
            }
            else
            {
                _simulator.Mouse.LeftButtonClick();
            }
        }
        else if (action.Type == InputType.KeyPress)
        {
            if (Enum.TryParse(action.Key, out VirtualKeyCode keyCode) || TryGetKeyCodeParse(action.Key, out keyCode))
            {
                _simulator.Keyboard.KeyPress(keyCode);
            }
        }
    }
    private bool TryGetKeyCodeParse(string str, out VirtualKeyCode keyCode)
    {
        keyCode = VirtualKeyCode.NONAME;
        if (!virtualKeyMap.TryGetValue(str, out keyCode))
            return false;

        return true;
    }
    public static Dictionary<string, VirtualKeyCode> GenerateVirtualKeyMap()
    {
        return Enum.GetValues(typeof(VirtualKeyCode))
            .Cast<VirtualKeyCode>()
            .Where(vk => vk.ToString().StartsWith("VK_"))
            .ToDictionary(vk => vk.ToString().Replace("VK_", ""), vk => vk);
    }
    private void MoveMouseSmoothly(int targetX, int targetY)
    {
        int startX = Cursor.Position.X;
        int startY = Cursor.Position.Y;

        int steps = 50; // 增加步驟數來使移動更順滑
        Random rand = new Random();

        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;  // 線性比例
            t = t * t * (3 - 2 * t); // 使用平滑曲線公式 (Ease-in-Ease-out)

            int newX = (int)(startX + t * (targetX - startX) + rand.Next(-2, 2)); // 加入微小隨機擾動
            int newY = (int)(startY + t * (targetY - startY) + rand.Next(-2, 2));

            Cursor.Position = new System.Drawing.Point(newX, newY);
            Thread.Sleep(rand.Next(1, 5)); // 每次移動時隨機延遲，讓動作更自然
        }

        Cursor.Position = new System.Drawing.Point(targetX, targetY); // 最後確保到達目標位置
    }

}
