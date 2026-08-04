using System.Windows;
using System.Windows.Input;
using GuitarInputController.App.ViewModels;

namespace GuitarInputController.App.Views;

/// <summary>
/// Modal dialog for advanced settings configuration.
/// Covers audio parameters, pitch detection, global hotkeys, and application behavior.
/// </summary>
public partial class AdvancedSettingsWindow : Window
{
    public AdvancedSettingsWindow()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
        {
            if (e.NewValue is AdvancedSettingsViewModel vm)
            {
                vm.CloseAction = () => DialogResult = true;
            }
        };
    }

    // ── Title bar drag ──────────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    // ── Close / cancel ──────────────────────────────────────────────

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
