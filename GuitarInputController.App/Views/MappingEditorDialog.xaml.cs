using System.Windows;
using System.Windows.Input;
using GuitarInputController.App.ViewModels;

namespace GuitarInputController.App.Views;

/// <summary>
/// Modal dialog for adding or editing a note-to-action mapping.
/// Supports key press, combination, and mouse click action types
/// with hold or pulse trigger modes.
/// </summary>
public partial class MappingEditorDialog : Window
{
    public MappingEditorDialog()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
        {
            if (e.NewValue is MappingEditorViewModel vm)
            {
                vm.CloseAction = () =>
                {
                    DialogResult = vm.Result != null;
                };
            }
        };
    }

    // ── Title bar drag ──────────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    // ── Close ────────────────────────────────────────────────────────

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    // ── Modifier checkbox toggling ────────────────────────────────────

    private void ModifierCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element &&
            element.DataContext is string modifier &&
            DataContext is MappingEditorViewModel vm)
        {
            vm.ToggleModifier(modifier);
        }
    }
}
