using System.Windows;
using System.Windows.Input;
using GuitarInputController.App.ViewModels;
using GuitarInputController.Core.Services;
using GuitarInputController.App;

namespace GuitarInputController.App.Views;

/// <summary>
/// Floating virtual keyboard visualization window. Displays a keyboard
/// layout with highlighted keys based on triggered mappings.
/// Supports dragging via the title bar and persists position on close.
/// </summary>
public partial class VirtualKeyboardWindow : Window
{
    private readonly ISettingsService _settingsService;

    public VirtualKeyboardWindow()
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
        if (settings.Appearance.KeyboardLeft > 0 || settings.Appearance.KeyboardTop > 0)
        {
            Left = settings.Appearance.KeyboardLeft;
            Top = settings.Appearance.KeyboardTop;
        }
        if (settings.Appearance.KeyboardWidth > 0)
            Width = settings.Appearance.KeyboardWidth;
        if (settings.Appearance.KeyboardHeight > 0)
            Height = settings.Appearance.KeyboardHeight;
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var settings = _settingsService.Load();
        settings.Appearance.KeyboardLeft = (int)Left;
        settings.Appearance.KeyboardTop = (int)Top;
        settings.Appearance.KeyboardWidth = (int)Width;
        settings.Appearance.KeyboardHeight = (int)Height;
        _settingsService.Save(settings);
    }
}
