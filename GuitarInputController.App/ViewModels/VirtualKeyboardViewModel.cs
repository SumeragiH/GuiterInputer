using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuitarInputController.Core.Enums;

namespace GuitarInputController.App.ViewModels;

/// <summary>
/// ViewModel for the virtual keyboard visualization window.
/// Displays a keyboard layout with highlighted keys based on
/// triggered mappings.
/// </summary>
public partial class VirtualKeyboardViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<string> _highlightedKeys = new();

    [ObservableProperty]
    private KeyboardLayoutType _keyboardLayout = KeyboardLayoutType.Key104;

    [ObservableProperty]
    private bool _windowTopMost = true;

    [ObservableProperty]
    private double _windowOpacity = 0.9;

    public VirtualKeyboardViewModel()
    {
    }

    /// <summary>
    /// Called when a mapping is triggered (key press or combination).
    /// Highlights the corresponding key(s) on the virtual keyboard.
    /// </summary>
    /// <param name="keyCodes">The key code strings to highlight (e.g. ["W"], ["Ctrl", "C"]).</param>
    public void OnMappingTriggered(IEnumerable<string> keyCodes)
    {
        RemoveExpiredHighlights();

        // If a large batch comes in quickly, just work with the latest set
        foreach (var key in keyCodes)
        {
            if (!HighlightedKeys.Contains(key))
            {
                HighlightedKeys.Add(key);
            }
        }
    }

    /// <summary>
    /// Called when a single key is pressed (hold-mode key down).
    /// </summary>
    public void OnKeyDown(string keyCode)
    {
        if (!HighlightedKeys.Contains(keyCode))
        {
            HighlightedKeys.Add(keyCode);
        }
    }

    /// <summary>
    /// Called when a single key is released (hold-mode key up).
    /// </summary>
    public void OnKeyUp(string keyCode)
    {
        HighlightedKeys.Remove(keyCode);
    }

    /// <summary>
    /// Clears the key highlights that have expired.
    /// For pulse-triggered highlights, this can be called on a timer from the view.
    /// </summary>
    [RelayCommand]
    private void ClearExpiredHighlights()
    {
        RemoveExpiredHighlights();
    }

    /// <summary>
    /// Clears all highlighted keys immediately.
    /// </summary>
    [RelayCommand]
    private void ClearAllHighlights()
    {
        HighlightedKeys.Clear();
    }

    /// <summary>
    /// Tracks the time each key was highlighted for pulse-mode auto-removal.
    /// </summary>
    private readonly Dictionary<string, DateTime> _keyHighlightTimes = new();

    /// <summary>
    /// Removes highlights that have been active for more than a configurable duration.
    /// </summary>
    private void RemoveExpiredHighlights(int expirationMs = 800)
    {
        var now = DateTime.UtcNow;
        var toRemove = new List<string>();

        foreach (var key in HighlightedKeys)
        {
            if (_keyHighlightTimes.TryGetValue(key, out var addedTime) &&
                (now - addedTime).TotalMilliseconds > expirationMs)
            {
                toRemove.Add(key);
            }
        }

        foreach (var key in toRemove)
        {
            HighlightedKeys.Remove(key);
            _keyHighlightTimes.Remove(key);
        }
    }
}
