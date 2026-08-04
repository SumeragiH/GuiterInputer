namespace GuitarInputController.Input.Interfaces;

/// <summary>
/// 输入模拟器接口 — 统一的键鼠模拟抽象
/// </summary>
public interface IInputSimulator
{
    /// <summary>按下键盘按键</summary>
    void KeyDown(ushort virtualKeyCode);

    /// <summary>释放键盘按键</summary>
    void KeyUp(ushort virtualKeyCode);

    /// <summary>按下并立即释放键盘按键（脉冲触发）</summary>
    void KeyPress(ushort virtualKeyCode);

    /// <summary>发送组合键</summary>
    void SendCombination(IEnumerable<ushort> modifierKeys, ushort keyCode);

    /// <summary>发送组合键（字符串形式，如 "Ctrl+C"）</summary>
    void SendCombination(IEnumerable<string> modifierKeys, string keyCode);

    /// <summary>鼠标按键点击</summary>
    void MouseClick(Core.Enums.MouseButtonType button);

    /// <summary>鼠标按键按下</summary>
    void MouseDown(Core.Enums.MouseButtonType button);

    /// <summary>鼠标按键释放</summary>
    void MouseUp(Core.Enums.MouseButtonType button);

    /// <summary>注册全局热键</summary>
    bool RegisterHotKey(IntPtr windowHandle, int id, uint modKeys, uint virtualKey);

    /// <summary>注销全局热键</summary>
    bool UnregisterHotKey(IntPtr windowHandle, int id);

    /// <summary>将字符串键码转换为虚拟键码</summary>
    ushort? StringToVirtualKey(string keyString);

    /// <summary>将修饰键字符串转换为虚拟键码</summary>
    ushort? ModifierStringToVirtualKey(string modifierString);
}
