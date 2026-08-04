namespace GuitarInputController.Audio.Models;

/// <summary>
/// 音频电平信息
/// </summary>
public class AudioLevelInfo
{
    /// <summary>峰值电平（0.0 ~ 1.0）</summary>
    public float PeakLevel { get; init; }

    /// <summary>RMS 电平（0.0 ~ 1.0）</summary>
    public float RmsLevel { get; init; }

    /// <summary>分贝值</summary>
    public float Decibels => RmsLevel > 0.0001f ? 20f * MathF.Log10(RmsLevel) : -80f;
}
