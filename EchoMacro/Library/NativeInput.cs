using System.Runtime.InteropServices;
using EchoMacro.Service;

namespace EchoMacro.Library
{
    /// <summary>Sends simulated keyboard and mouse input through the Win32 SendInput API.</summary>
    public static class NativeInput
    {
        public const ushort VK_SHIFT = 0x10;
        public const ushort VK_CONTROL = 0x11;
        public const ushort VK_MENU = 0x12;
        public const ushort VK_LWIN = 0x5B;
        public const ushort VK_RWIN = 0x5C;

        public static void KeyDown(ushort virtualKey) => SendKey(virtualKey, keyUp: false);
        public static void KeyUp(ushort virtualKey) => SendKey(virtualKey, keyUp: true);

        /// <summary>True while the key is physically or virtually held down right now.</summary>
        public static bool IsKeyDown(ushort virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

        public static void MouseButton(MouseButtonKind button, bool down)
        {
            (uint flag, uint data) = button switch
            {
                MouseButtonKind.Right => (down ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP, 0u),
                MouseButtonKind.Middle => (down ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP, 0u),
                MouseButtonKind.XButton1 => (down ? MOUSEEVENTF_XDOWN : MOUSEEVENTF_XUP, XBUTTON1),
                MouseButtonKind.XButton2 => (down ? MOUSEEVENTF_XDOWN : MOUSEEVENTF_XUP, XBUTTON2),
                _ => (down ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP, 0u),
            };
            SendMouse(flag, data);
        }

        /// <summary>Scrolls the wheel at the current cursor position; 120 is one notch away from the user.</summary>
        public static void MouseWheel(int delta) => SendMouse(MOUSEEVENTF_WHEEL, unchecked((uint)delta));

        /// <summary>Raises the system timer to 1 ms ticks so short sleeps are accurate. Pair with <see cref="EndHighResolutionTimer"/>.</summary>
        public static void BeginHighResolutionTimer() => timeBeginPeriod(1);
        public static void EndHighResolutionTimer() => timeEndPeriod(1);

        private static void SendKey(ushort virtualKey, bool keyUp)
        {
            uint flags = keyUp ? KEYEVENTF_KEYUP : 0;
            if (IsExtendedKey(virtualKey))
                flags |= KEYEVENTF_EXTENDEDKEY;

            var input = new INPUT { type = INPUT_KEYBOARD };
            input.data.keyboard = new KEYBDINPUT
            {
                wVk = virtualKey,
                wScan = (ushort)MapVirtualKey(virtualKey, MAPVK_VK_TO_VSC),
                dwFlags = flags
            };
            Send(input);
        }

        private static void SendMouse(uint flags, uint data)
        {
            var input = new INPUT { type = INPUT_MOUSE };
            input.data.mouse = new MOUSEINPUT { dwFlags = flags, mouseData = data };
            Send(input);
        }

        private static void Send(INPUT input) => SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());

        /// <summary>
        /// Keys that share a scan code with a numeric-keypad key or a left-hand twin. Without the extended flag
        /// Windows treats e.g. an arrow key as its keypad counterpart, which breaks Shift+arrow selection.
        /// </summary>
        private static bool IsExtendedKey(ushort virtualKey) => virtualKey switch
        {
            0x03 => true, // VK_CANCEL
            0x21 or 0x22 or 0x23 or 0x24 => true, // Page Up, Page Down, End, Home
            0x25 or 0x26 or 0x27 or 0x28 => true, // Arrow keys
            0x2C or 0x2D or 0x2E => true, // Print Screen, Insert, Delete
            0x5B or 0x5C or 0x5D => true, // Left Win, Right Win, Apps
            0x6F => true, // Numpad divide
            0x90 => true, // Num Lock
            0xA3 or 0xA5 => true, // Right Ctrl, Right Alt
            _ => false,
        };

        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;

        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint MAPVK_VK_TO_VSC = 0;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_XDOWN = 0x0080;
        private const uint MOUSEEVENTF_XUP = 0x0100;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint XBUTTON1 = 0x0001;
        private const uint XBUTTON2 = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion data;
        }

        //The native union is as large as its largest member (MOUSEINPUT), which this layout reproduces.
        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mouse;
            [FieldOffset(0)] public KEYBDINPUT keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint cInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint uPeriod);
    }
}
