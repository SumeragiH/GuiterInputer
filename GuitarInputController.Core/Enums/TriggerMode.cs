namespace GuitarInputController.Core.Enums;

/// <summary>
/// 音符触发模式
/// </summary>
public enum TriggerMode
{
    /// <summary>
    /// 按住模式：Note On = Key Down, Note Off = Key Up
    /// 适合游戏 WASD 移动等持续操作
    /// </summary>
    Hold,

    /// <summary>
    /// 脉冲模式：检测到音符立即触发一次按键按下+释放
    /// 适合组合键快捷操作
    /// </summary>
    Pulse
}
