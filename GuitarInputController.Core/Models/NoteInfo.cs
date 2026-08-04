namespace GuitarInputController.Core.Models;

/// <summary>
/// 音符信息（科学音高记谱法）
/// </summary>
public class NoteInfo
{
    /// <summary>音名（如 C, C#, D, D#, E, F, F#, G, G#, A, A#, B）</summary>
    public string NoteName { get; init; } = string.Empty;

    /// <summary>八度（如 2, 3, 4）</summary>
    public int Octave { get; init; }

    /// <summary>频率（Hz）</summary>
    public double Frequency { get; init; }

    /// <summary>完整的科学音高记谱法名称（如 "C4", "E2"）</summary>
    public string FullName => $"{NoteName}{Octave}";

    /// <summary>MIDI 音符编号（A4 = 69）</summary>
    public int MidiNoteNumber { get; init; }

    public override string ToString() => FullName;

    public override bool Equals(object? obj)
    {
        if (obj is NoteInfo other)
            return FullName == other.FullName;
        return false;
    }

    public override int GetHashCode() => FullName.GetHashCode();
}
