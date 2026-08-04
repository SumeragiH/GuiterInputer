namespace GuitarInputController.Core.Models;

/// <summary>
/// 音频采集设置
/// </summary>
public class AudioSettings
{
    /// <summary>输入设备 ID</summary>
    public string InputDeviceId { get; set; } = string.Empty;

    /// <summary>采样率（默认 44100Hz）</summary>
    public int SampleRate { get; set; } = 44100;

    /// <summary>缓冲区大小（毫秒）</summary>
    public int BufferSizeMs { get; set; } = 40;

    /// <summary>输入增益（dB），默认 0dB，范围 -20 ~ +20dB</summary>
    public double InputGainDb { get; set; } = 0.0;
}
