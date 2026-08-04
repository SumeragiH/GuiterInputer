namespace GuitarInputController.Core.Enums;

/// <summary>
/// 映射动作类型
/// </summary>
public enum ActionType
{
    /// <summary>单个键盘按键</summary>
    KeyPress,

    /// <summary>组合键（带修饰键）</summary>
    Combination,

    /// <summary>鼠标按键点击</summary>
    MouseClick
}
