using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace EchoMacro.Service
{
    public class GlobalHotKeyManager : IDisposable
    {
        private readonly IntPtr _windowHandle;
        private readonly HwndSource _source;
        private readonly Dictionary<int, Action> _hotKeyActions = new Dictionary<int, Action>();

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_NOREPEAT = 0x4000;

        public GlobalHotKeyManager(Window window)
        {
            _windowHandle = new WindowInteropHelper(window).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(WndProc);
        }

        /// <summary>Registers a system-wide hotkey. Returns false when another application already owns it.</summary>
        public bool RegisterHotKey(int id, ModifierKeys modifiers, Key key, Action callback)
        {
            if (_hotKeyActions.ContainsKey(id)) return false;

            //ModifierKeys uses the same bit values as the Win32 MOD_* flags.
            uint fsModifiers = (uint)modifiers | MOD_NOREPEAT;
            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            if (!RegisterHotKey(_windowHandle, id, fsModifiers, vk))
                return false;

            _hotKeyActions[id] = callback;
            return true;
        }

        public void UnregisterHotKey(int id)
        {
            if (_hotKeyActions.ContainsKey(id))
            {
                UnregisterHotKey(_windowHandle, id);
                _hotKeyActions.Remove(id);
            }
        }

        public void UnregisterAllHotKeys()
        {
            foreach (var id in _hotKeyActions.Keys)
            {
                UnregisterHotKey(_windowHandle, id);
            }
            _hotKeyActions.Clear();
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_hotKeyActions.TryGetValue(id, out var action))
                {
                    action.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterAllHotKeys();
            _source.RemoveHook(WndProc);
        }
    }
}
