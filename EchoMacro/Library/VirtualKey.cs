using WindowsInput.Native;

namespace EchoMacro.Library
{
    public static class VirtualKey
    {
        public static Dictionary<string, VirtualKeyCode> GetVirtualKeyMap()
        {
            var result = Enum.GetValues(typeof(VirtualKeyCode))
                .Cast<VirtualKeyCode>()
                .Where(vk => vk.ToString().StartsWith("VK_"))
                .ToDictionary(vk => vk.ToString().Replace("VK_", ""), vk => vk);

            var additionalMappings = new Dictionary<string, VirtualKeyCode>
        {
            { "SPACE", VirtualKeyCode.SPACE },
            { "ENTER", VirtualKeyCode.RETURN },
            { "ESC", VirtualKeyCode.ESCAPE },
            { "TAB", VirtualKeyCode.TAB },
            { "Back", VirtualKeyCode.BACK },
            { "Clear", VirtualKeyCode.CLEAR },
            { "Return", VirtualKeyCode.RETURN },
            { "ShiftKey", VirtualKeyCode.SHIFT },
            { "ControlKey", VirtualKeyCode.CONTROL },
            { "Menu", VirtualKeyCode.MENU },
            { "Pause", VirtualKeyCode.PAUSE },
            { "Capital", VirtualKeyCode.CAPITAL },
            { "KanaMode", VirtualKeyCode.KANA },
            { "JunjaMode", VirtualKeyCode.JUNJA },
            { "FinalMode", VirtualKeyCode.FINAL },
            { "HanjaMode", VirtualKeyCode.HANJA },
            { "Escape", VirtualKeyCode.ESCAPE },
            { "IMEConvert", VirtualKeyCode.CONVERT },
            { "IMENonconvert", VirtualKeyCode.NONCONVERT },
            { "IMEAccept", VirtualKeyCode.ACCEPT },
            { "Space", VirtualKeyCode.SPACE },
            { "Prior", VirtualKeyCode.PRIOR },
            { "Next", VirtualKeyCode.NEXT },
            { "End", VirtualKeyCode.END },
            { "Home", VirtualKeyCode.HOME },
            { "Left", VirtualKeyCode.LEFT },
            { "Up", VirtualKeyCode.UP },
            { "Right", VirtualKeyCode.RIGHT },
            { "Down", VirtualKeyCode.DOWN },
            { "Select", VirtualKeyCode.SELECT },
            { "Print", VirtualKeyCode.PRINT },
            { "Execute", VirtualKeyCode.EXECUTE },
            { "Snapshot", VirtualKeyCode.SNAPSHOT },
            { "Insert", VirtualKeyCode.INSERT },
            { "Delete", VirtualKeyCode.DELETE },
            { "Help", VirtualKeyCode.HELP },
            { "LWin", VirtualKeyCode.LWIN },
            { "RWin", VirtualKeyCode.RWIN },
            { "Apps", VirtualKeyCode.APPS },
            { "Sleep", VirtualKeyCode.SLEEP },
            { "Numpad0", VirtualKeyCode.NUMPAD0 },
            { "Numpad1", VirtualKeyCode.NUMPAD1 },
            { "Numpad2", VirtualKeyCode.NUMPAD2 },
            { "Numpad3", VirtualKeyCode.NUMPAD3 },
            { "Numpad4", VirtualKeyCode.NUMPAD4 },
            { "Numpad5", VirtualKeyCode.NUMPAD5 },
            { "Numpad6", VirtualKeyCode.NUMPAD6 },
            { "Numpad7", VirtualKeyCode.NUMPAD7 },
            { "Numpad8", VirtualKeyCode.NUMPAD8 },
            { "Numpad9", VirtualKeyCode.NUMPAD9 },
            { "D0", VirtualKeyCode.VK_0 },
            { "D1", VirtualKeyCode.VK_1 },
            { "D2", VirtualKeyCode.VK_2 },
            { "D3", VirtualKeyCode.VK_3 },
            { "D4", VirtualKeyCode.VK_4 },
            { "D5", VirtualKeyCode.VK_5 },
            { "D6", VirtualKeyCode.VK_6 },
            { "D7", VirtualKeyCode.VK_7 },
            { "D8", VirtualKeyCode.VK_8 },
            { "D9", VirtualKeyCode.VK_9 },
            { "Multiply", VirtualKeyCode.MULTIPLY },
            { "Add", VirtualKeyCode.ADD },
            { "Separator", VirtualKeyCode.SEPARATOR },
            { "Subtract", VirtualKeyCode.SUBTRACT },
            { "Decimal", VirtualKeyCode.DECIMAL },
            { "Divide", VirtualKeyCode.DIVIDE },
            { "F1", VirtualKeyCode.F1 },
            { "F2", VirtualKeyCode.F2 },
            { "F3", VirtualKeyCode.F3 },
            { "F4", VirtualKeyCode.F4 },
            { "F5", VirtualKeyCode.F5 },
            { "F6", VirtualKeyCode.F6 },
            { "F7", VirtualKeyCode.F7 },
            { "F8", VirtualKeyCode.F8 },
            { "F9", VirtualKeyCode.F9 },
            { "F10", VirtualKeyCode.F10 },
            { "F11", VirtualKeyCode.F11 },
            { "F12", VirtualKeyCode.F12 },
            { "Scroll", VirtualKeyCode.SCROLL },
            { "LShiftKey", VirtualKeyCode.LSHIFT },
            { "RShiftKey", VirtualKeyCode.RSHIFT },
            { "LControlKey", VirtualKeyCode.LCONTROL },
            { "RControlKey", VirtualKeyCode.RCONTROL },
            { "LMenu", VirtualKeyCode.LMENU },
            { "RMenu", VirtualKeyCode.RMENU },
            { "BrowserBack", VirtualKeyCode.BROWSER_BACK },
            { "BrowserForward", VirtualKeyCode.BROWSER_FORWARD },
            { "BrowserRefresh", VirtualKeyCode.BROWSER_REFRESH },
            { "BrowserStop", VirtualKeyCode.BROWSER_STOP },
            { "BrowserSearch", VirtualKeyCode.BROWSER_SEARCH },
            { "BrowserFavorites", VirtualKeyCode.BROWSER_FAVORITES },
            { "BrowserHome", VirtualKeyCode.BROWSER_HOME },
            { "VolumeMute", VirtualKeyCode.VOLUME_MUTE },
            { "VolumeDown", VirtualKeyCode.VOLUME_DOWN },
            { "VolumeUp", VirtualKeyCode.VOLUME_UP },
            { "MediaNextTrack", VirtualKeyCode.MEDIA_NEXT_TRACK },
            { "MediaPreviousTrack", VirtualKeyCode.MEDIA_PREV_TRACK },
            { "MediaStop", VirtualKeyCode.MEDIA_STOP },
            { "MediaPlayPause", VirtualKeyCode.MEDIA_PLAY_PAUSE },
            { "LaunchMail", VirtualKeyCode.LAUNCH_MAIL },
            { "SelectMedia", VirtualKeyCode.LAUNCH_MEDIA_SELECT },
            { "LaunchApplication1", VirtualKeyCode.LAUNCH_APP1 },
            { "LaunchApplication2", VirtualKeyCode.LAUNCH_APP2 }
        };

            foreach (var item in additionalMappings)
            {
                result.Add(item.Key, item.Value);
            }

            return result;
        }
    }
}
