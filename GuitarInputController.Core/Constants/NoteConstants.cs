namespace GuitarInputController.Core.Constants;

/// <summary>
/// 音符频率对照表与相关常量
/// 使用科学音高记谱法，A4 = 440Hz 为基准
/// </summary>
public static class NoteConstants
{
    /// <summary>十二平均律中每半音的频率比</summary>
    public const double SemitoneRatio = 1.0594630943592953;

    /// <summary>标准音 A4 的 MIDI 编号</summary>
    public const int A4MidiNumber = 69;

    /// <summary>标准音 A4 的默认频率</summary>
    public const double A4DefaultFrequency = 440.0;

    /// <summary>所有音名</summary>
    public static readonly string[] NoteNames =
        ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    /// <summary>吉他标准调音各弦开放弦（从最细第1弦到最粗第6弦）</summary>
    public static readonly (int StringNumber, string NoteName, int Octave)[] StandardTuning =
    [
        (1, "E", 4),  // 第 1 弦（最细）
        (2, "B", 3),  // 第 2 弦
        (3, "G", 3),  // 第 3 弦
        (4, "D", 3),  // 第 4 弦
        (5, "A", 2),  // 第 5 弦
        (6, "E", 2),  // 第 6 弦（最粗）
    ];

    /// <summary>默认指板品数</summary>
    public const int DefaultFretCount = 24;

    /// <summary>
    /// 根据 MIDI 编号获取音名和八度
    /// </summary>
    public static (string NoteName, int Octave) MidiToNote(int midiNumber)
    {
        int noteIndex = midiNumber % 12;
        int octave = midiNumber / 12 - 1;
        return (NoteNames[noteIndex], octave);
    }

    /// <summary>
    /// 根据音名和八度获取 MIDI 编号
    /// </summary>
    public static int NoteToMidi(string noteName, int octave)
    {
        int noteIndex = Array.IndexOf(NoteNames, noteName);
        if (noteIndex < 0)
            throw new ArgumentException($"无效的音名: {noteName}");
        return (octave + 1) * 12 + noteIndex;
    }

    /// <summary>
    /// 计算给定 MIDI 编号的频率（基于 A4 频率）
    /// </summary>
    public static double MidiToFrequency(int midiNumber, double a4Frequency = A4DefaultFrequency)
    {
        int semitoneOffset = midiNumber - A4MidiNumber;
        return a4Frequency * Math.Pow(SemitoneRatio, semitoneOffset);
    }

    /// <summary>
    /// 根据频率估算最近的 MIDI 编号
    /// </summary>
    public static int FrequencyToMidi(double frequency, double a4Frequency = A4DefaultFrequency)
    {
        if (frequency <= 0)
            return -1;
        double semitonesFromA4 = 12.0 * Math.Log2(frequency / a4Frequency);
        return (int)Math.Round(semitonesFromA4 + A4MidiNumber);
    }
}
