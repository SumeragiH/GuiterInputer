namespace GuitarInputController.Audio.Models;

/// <summary>
/// 音高检测结果
/// </summary>
public class PitchResult
{
    /// <summary>检测到的频率（Hz），-1 表示未检测到</summary>
    public double Frequency { get; init; } = -1;

    /// <summary>音名（如 C, D, E）</summary>
    public string NoteName { get; init; } = string.Empty;

    /// <summary>八度</summary>
    public int Octave { get; init; }

    /// <summary>完整的科学音高记谱法名称</summary>
    public string FullName => $"{NoteName}{Octave}";

    /// <summary>MIDI 音符编号</summary>
    public int MidiNoteNumber { get; init; }

    /// <summary>检测置信度（0.0 ~ 1.0）</summary>
    public double Confidence { get; init; }

    /// <summary>音量级别（0.0 ~ 1.0）</summary>
    public double Volume { get; init; }

    /// <summary>是否为有效音符（通过置信度和音量阈值）</summary>
    public bool IsValid => Confidence > 0 && Frequency > 0;
}
