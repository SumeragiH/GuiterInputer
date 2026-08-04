namespace GuitarInputController.Core.Models;

/// <summary>
/// 音高检测参数设置
/// </summary>
public class PitchDetectionSettings
{
    /// <summary>标准音 A4 频率（默认 440Hz）</summary>
    public double A4Frequency { get; set; } = 440.0;

    /// <summary>音量检测阈值（0.0 ~ 1.0），低于此值视为噪音</summary>
    public double VolumeThreshold { get; set; } = 0.05;

    /// <summary>最小音符持续时长（毫秒），短于此值视为噪音</summary>
    public int MinNoteDurationMs { get; set; } = 30;

    /// <summary>音高检测置信度阈值（0.0 ~ 1.0），低于此值的结果丢弃</summary>
    public double ConfidenceThreshold { get; set; } = 0.7;

    /// <summary>最小输入间隔（毫秒），连续相同音符的去重间隔</summary>
    public int MinInputIntervalMs { get; set; } = 50;
}
