public enum InputType { MouseClick, KeyPress }

public class RecordedAction
{
    public InputType Type { get; set; }
    public double Timestamp { get; set; }
    public int X { get; set; }  // 滑鼠X座標
    public int Y { get; set; }  // 滑鼠Y座標
    public string Key { get; set; } // 按鍵
    public bool IsRightClick { get; set; } // 是否為右鍵
}
