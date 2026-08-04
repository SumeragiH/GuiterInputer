namespace GuitarInputController.Core.Models;

/// <summary>
/// 全局热键设置
/// </summary>
public class HotKeySettings
{
    /// <summary>启用/禁用吉他输入的全局热键</summary>
    public string ToggleInput { get; set; } = "Ctrl+Shift+G";

    /// <summary>切换映射方案的全局热键</summary>
    public string SwitchProfile { get; set; } = "Ctrl+Shift+P";
}
