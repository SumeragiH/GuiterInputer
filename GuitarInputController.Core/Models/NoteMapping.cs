using GuitarInputController.Core.Enums;

namespace GuitarInputController.Core.Models;

/// <summary>
/// 单条音符到键鼠的映射配置
/// </summary>
public class NoteMapping
{
    /// <summary>映射的唯一标识</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>源音符（如 "C4"）</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>动作类型</summary>
    public ActionType ActionType { get; set; } = ActionType.KeyPress;

    /// <summary>目标按键代码（如 "W", "A", "Space"）</summary>
    public string? KeyCode { get; set; }

    /// <summary>修饰键列表（如 ["Ctrl"], ["Ctrl", "Shift"]）</summary>
    public List<string> ModifierKeys { get; set; } = new();

    /// <summary>触发模式</summary>
    public TriggerMode TriggerMode { get; set; } = TriggerMode.Hold;

    /// <summary>鼠标按键（当 ActionType 为 MouseClick 时可用）</summary>
    public MouseButtonType? MouseButton { get; set; }

    /// <summary>可读的描述标签</summary>
    public string? Label { get; set; }

    /// <summary>生成此映射的可读描述</summary>
    public string GetDescription()
    {
        var triggerLabel = TriggerMode == TriggerMode.Hold ? "按住" : "脉冲";
        var actionDesc = ActionType switch
        {
            ActionType.KeyPress => $"按键 {KeyCode}",
            ActionType.Combination => $"组合键 {string.Join("+", ModifierKeys)}{(KeyCode != null ? "+" + KeyCode : "")}",
            ActionType.MouseClick => $"鼠标{GetMouseButtonName(MouseButton)}",
            _ => "未知操作"
        };
        return $"[{triggerLabel}] {Note} → {actionDesc}";
    }

    private static string GetMouseButtonName(MouseButtonType? button) => button switch
    {
        MouseButtonType.Left => "左键",
        MouseButtonType.Right => "右键",
        MouseButtonType.Middle => "中键",
        MouseButtonType.XButton1 => "侧键1",
        MouseButtonType.XButton2 => "侧键2",
        _ => "未知按键"
    };
}
