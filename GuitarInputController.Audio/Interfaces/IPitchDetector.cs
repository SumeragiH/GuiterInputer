using GuitarInputController.Audio.Models;

namespace GuitarInputController.Audio.Interfaces;

/// <summary>
/// 音高检测器接口
/// </summary>
public interface IPitchDetector
{
    /// <summary>检测音频缓冲区中的音高</summary>
    /// <param name="samples">单声道 PCM 采样（float, -1.0 ~ 1.0）</param>
    /// <param name="sampleRate">采样率</param>
    /// <returns>检测结果（可能为 null，表示未检测到音符）</returns>
    PitchResult? DetectPitch(float[] samples, int sampleRate);

    /// <summary>置信度阈值（低于此值的结果将被丢弃）</summary>
    double ConfidenceThreshold { get; set; }

    /// <summary>音量阈值（低于此值的信号视为噪音）</summary>
    double VolumeThreshold { get; set; }
}
