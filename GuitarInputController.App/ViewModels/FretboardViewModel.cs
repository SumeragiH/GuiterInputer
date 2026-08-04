using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuitarInputController.Core.Extensions;

namespace GuitarInputController.App.ViewModels;

/// <summary>
/// ViewModel for the fretboard visualization window.
/// Displays a guitar fretboard with highlighted note positions based on
/// the currently detected pitch.
/// </summary>
public partial class FretboardViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<string> _highlightedNotes = new();

    [ObservableProperty]
    private ObservableCollection<(int StringIndex, int Fret)> _highlightedPositions = new();

    [ObservableProperty]
    private int _stringCount = 6;

    [ObservableProperty]
    private int _fretCount = 24;

    [ObservableProperty]
    private bool _windowTopMost = true;

    [ObservableProperty]
    private double _windowOpacity = 0.9;

    [ObservableProperty]
    private bool _isLocked;

    public FretboardViewModel()
    {
    }

    /// <summary>
    /// Called by the parent ViewModel when a new pitch is detected.
    /// Updates the highlighted fretboard positions for the given note.
    /// </summary>
    /// <param name="noteFullName">The note in scientific pitch notation (e.g. "C4").</param>
    public void OnPitchDetected(string noteFullName)
    {
        // Clear previous highlights
        HighlightedNotes.Clear();
        HighlightedPositions.Clear();

        if (string.IsNullOrEmpty(noteFullName)) return;

        // Parse note name and octave from full name
        var (noteName, octave) = ParseNoteFullName(noteFullName);
        if (noteName == null) return;

        // Build a temporary NoteInfo for lookup
        // We use the NoteExtensions helper to find fretboard positions
        var positions = NoteExtensions.GetFretboardPositions(
            new Core.Models.NoteInfo
            {
                NoteName = noteName,
                Octave = octave
            },
            StringCount,
            FretCount);

        foreach (var pos in positions)
        {
            HighlightedPositions.Add(pos);
        }

        HighlightedNotes.Add(noteFullName);
    }

    /// <summary>
    /// Clears all highlighted positions when no note is detected.
    /// </summary>
    [RelayCommand]
    private void ClearHighlights()
    {
        HighlightedNotes.Clear();
        HighlightedPositions.Clear();
    }

    /// <summary>
    /// Parses a note full name like "C4" or "C#4" into its name and octave components.
    /// </summary>
    private static (string? NoteName, int Octave) ParseNoteFullName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName) || fullName.Length < 2)
            return (null, 0);

        // Sharp notes have 3 chars (e.g. "C#4"), natural notes have 2 (e.g. "C4")
        string noteName;
        string octaveStr;

        if (fullName.Length >= 3 && fullName[1] == '#')
        {
            noteName = fullName[..2];
            octaveStr = fullName[2..];
        }
        else
        {
            noteName = fullName[..1];
            octaveStr = fullName[1..];
        }

        if (int.TryParse(octaveStr, out var octave))
        {
            return (noteName, octave);
        }

        return (null, 0);
    }
}
