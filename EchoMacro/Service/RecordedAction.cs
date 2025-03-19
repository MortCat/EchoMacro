namespace EchoMacro.Service
{
    public enum InputType { MouseClick, KeyPress }

    public class RecordedAction
    {
        public InputType Type { get; set; }
        public double Timestamp { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Key { get; set; }
        public bool IsRightClick { get; set; }
    }
}
