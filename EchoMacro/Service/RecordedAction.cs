namespace EchoMacro.Service
{
    /// <summary>
    /// Kind of a recorded input. Values are stored as numbers in the JSON files, so only append new ones.
    /// MouseClick and KeyPress are the legacy "down and up in one action" kinds written by older versions.
    /// </summary>
    public enum InputType { MouseClick, KeyPress, KeyDown, KeyUp, MouseDown, MouseUp, MouseWheel, MouseMove }

    public enum MouseButtonKind { Left, Right, Middle, XButton1, XButton2 }

    public class RecordedAction
    {
        public InputType Type { get; set; }
        public double Timestamp { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        /// <summary>Key name (System.Windows.Forms.Keys). Kept for readability and for legacy files.</summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>Windows virtual-key code. 0 in legacy files, which only carry <see cref="Key"/>.</summary>
        public int KeyCode { get; set; }

        /// <summary>Legacy flag, only meaningful for <see cref="InputType.MouseClick"/>.</summary>
        public bool IsRightClick { get; set; }
        /// <summary>Button for <see cref="InputType.MouseDown"/> and <see cref="InputType.MouseUp"/>.</summary>
        public MouseButtonKind Button { get; set; }
        /// <summary>Wheel movement for <see cref="InputType.MouseWheel"/>; 120 is one notch away from the user.</summary>
        public int WheelDelta { get; set; }
    }
}
