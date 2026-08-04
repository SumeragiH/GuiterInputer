using System.Runtime.InteropServices;

namespace GuitarInputController.Input.Native;

/// <summary>
/// Windows API P/Invoke 声明
/// </summary>
internal static class NativeMethods
{
    #region SendInput

    public const int INPUT_KEYBOARD = 1;
    public const int INPUT_MOUSE = 0;

    public const uint KEYEVENTF_KEYDOWN = 0x0000;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    public const uint MOUSEEVENTF_XDOWN = 0x0080;
    public const uint MOUSEEVENTF_XUP = 0x0100;

    public const uint XBUTTON1 = 0x0001;
    public const uint XBUTTON2 = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public int type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    #endregion

    #region Global HotKey

    public const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    #endregion

    #region Modifier Keys

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    #endregion

    #region Virtual Key Codes (常用)

    /// <summary>将字符串键码（如 "W", "Space", "Enter"）转换为虚拟键码</summary>
    public static ushort? KeyStringToVk(string key)
    {
        return key switch
        {
            // 字母
            "A" => 0x41, "B" => 0x42, "C" => 0x43, "D" => 0x44, "E" => 0x45,
            "F" => 0x46, "G" => 0x47, "H" => 0x48, "I" => 0x49, "J" => 0x4A,
            "K" => 0x4B, "L" => 0x4C, "M" => 0x4D, "N" => 0x4E, "O" => 0x4F,
            "P" => 0x50, "Q" => 0x51, "R" => 0x52, "S" => 0x53, "T" => 0x54,
            "U" => 0x55, "V" => 0x56, "W" => 0x57, "X" => 0x58, "Y" => 0x59,
            "Z" => 0x5A,

            // 数字
            "0" => 0x30, "1" => 0x31, "2" => 0x32, "3" => 0x33, "4" => 0x34,
            "5" => 0x35, "6" => 0x36, "7" => 0x37, "8" => 0x38, "9" => 0x39,

            // 功能键
            "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
            "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
            "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,

            // 特殊键
            "Space" => 0x20, "Enter" => 0x0D, "Escape" => 0x1B, "Tab" => 0x09,
            "Backspace" => 0x08, "Delete" => 0x2E, "Insert" => 0x2D,
            "Home" => 0x24, "End" => 0x23, "PageUp" => 0x21, "PageDown" => 0x22,

            // 方向键
            "Left" => 0x25, "Up" => 0x26, "Right" => 0x27, "Down" => 0x28,

            // 修饰键
            "Ctrl" => 0x11, "Alt" => 0x12, "Shift" => 0x10, "Win" => 0x5B,

            // 其他
            "PrintScreen" => 0x2C, "ScrollLock" => 0x91, "Pause" => 0x13,
            "CapsLock" => 0x14, "NumLock" => 0x90,

            // 数字键盘
            "NumPad0" => 0x60, "NumPad1" => 0x61, "NumPad2" => 0x62,
            "NumPad3" => 0x63, "NumPad4" => 0x64, "NumPad5" => 0x65,
            "NumPad6" => 0x66, "NumPad7" => 0x67, "NumPad8" => 0x68,
            "NumPad9" => 0x69,
            "NumPadAdd" => 0x6B, "NumPadSubtract" => 0x6D,
            "NumPadMultiply" => 0x6A, "NumPadDivide" => 0x6F,
            "NumPadDecimal" => 0x6E,

            _ => null
        };
    }

    public static ushort? ModifierStringToVk(string modifier)
    {
        return modifier switch
        {
            "Ctrl" => 0x11,
            "Alt" => 0x12,
            "Shift" => 0x10,
            "Win" => 0x5B,
            _ => null
        };
    }

    public static uint ModifierStringToFlag(string modifier)
    {
        return modifier switch
        {
            "Ctrl" => MOD_CONTROL,
            "Alt" => MOD_ALT,
            "Shift" => MOD_SHIFT,
            "Win" => MOD_WIN,
            _ => 0
        };
    }

    #endregion
}
