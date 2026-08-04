using System.Windows;
using System.Windows.Input;
using GuitarInputController.App.ViewModels;
using GuitarInputController.Core.Services;
using GuitarInputController.App;

namespace GuitarInputController.App.Views;

/// <summary>
/// Floating fretboard visualization window. Displays a guitar fretboard
/// with note position highlights based on the currently detected pitch.
/// Supports dragging via the title bar and persists position on close.
/// </summary>
public partial class FretboardWindow : Window
{
    private readonly ISettingsService _settingsService;

    public FretboardWindow()
    {
        InitializeComponent();

        _settingsService = AppHost.GetService<ISettingsService>();

        // Restore saved position
        RestorePosition();

        Closing += OnWindowClosing;
    }

    // ── Drag ─────────────────────────────────────────────────────────

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    // ── Close ────────────────────────────────────────────────────────

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // ── Position persistence ─────────────────────────────────────────

    private void RestorePosition()
    {
        var settings = _settingsService.Load();
        if (settings.Appearance.FretboardLeft > 0 || settings.Appearance.FretboardTop > 0)
        {
            Left = settings.Appearance.FretboardLeft;
            Top = settings.Appearance.FretboardTop;
        }
        if (settings.Appearance.FretboardWidth > 0)
            Width = settings.Appearance.FretboardWidth;
        if (settings.Appearance.FretboardHeight > 0)
            Height = settings.Appearance.FretboardHeight;
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var settings = _settingsService.Load();
        settings.Appearance.FretboardLeft = (int)Left;
        settings.Appearance.FretboardTop = (int)Top;
        settings.Appearance.FretboardWidth = (int)Width;
        settings.Appearance.FretboardHeight = (int)Height;
        _settingsService.Save(settings);
    }
}
