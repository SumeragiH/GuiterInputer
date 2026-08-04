namespace GuitarInputController.Audio.Models;

/// <summary>
/// 音频配置
/// </summary>
public class AudioConfig
{
    public int SampleRate { get; set; } = 44100;
    public int BufferSizeMs { get; set; } = 40;
    public int Channels { get; set; } = 1;
}
