using GuitarInputController.Core.Constants;
using GuitarInputController.Core.Models;

namespace GuitarInputController.Core.Extensions;

/// <summary>
/// 音符相关扩展方法
/// </summary>
public static class NoteExtensions
{
    /// <summary>
    /// 获取吉他在某品某弦上的音高
    /// </summary>
    /// <param name="stringIndex">弦索引（0 = 第1弦最细，5 = 第6弦最粗）</param>
    /// <param name="fret">品数（0 = 开放弦）</param>
    /// <returns>该位置的音符信息</returns>
    public static NoteInfo GetNoteAtFret(int stringIndex, int fret)
    {
        if (stringIndex < 0 || stringIndex >= NoteConstants.StandardTuning.Length)
            throw new ArgumentOutOfRangeException(nameof(stringIndex), $"弦索引必须在 0-{NoteConstants.StandardTuning.Length - 1} 之间");

        var (_, openNoteName, openOctave) = NoteConstants.StandardTuning[stringIndex];
        int openMidi = NoteConstants.NoteToMidi(openNoteName, openOctave);
        int frettedMidi = openMidi + fret;
        var (noteName, octave) = NoteConstants.MidiToNote(frettedMidi);
        double frequency = NoteConstants.MidiToFrequency(frettedMidi);

        return new NoteInfo
        {
            NoteName = noteName,
            Octave = octave,
            Frequency = frequency,
            MidiNoteNumber = frettedMidi
        };
    }

    /// <summary>
    /// 获取指板上该音高出现的所有位置（弦, 品）
    /// 用于在指板窗口上高亮显示
    /// </summary>
    public static List<(int StringIndex, int Fret)> GetFretboardPositions(NoteInfo note, int stringCount = 6, int fretCount = 24)
    {
        var positions = new List<(int StringIndex, int Fret)>();

        for (int s = 0; s < stringCount; s++)
        {
            for (int f = 0; f <= fretCount; f++)
            {
                var noteAtPosition = GetNoteAtFret(s, f);
                if (noteAtPosition.FullName == note.FullName)
                {
                    positions.Add((s, f));
                }
            }
        }

        return positions;
    }
}
