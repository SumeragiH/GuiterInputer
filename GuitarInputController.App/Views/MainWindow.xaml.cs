using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using GuitarInputController.App.ViewModels;
using GuitarInputController.Core.Models;
using GuitarInputController.Core.Services;
using GuitarInputController.App;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace GuitarInputController.App.Views;

/// <summary>
/// Main application window. Handles custom chrome dragging, minimize-to-tray,
/// sub-window lifecycle (fretboard, virtual keyboard), and view-layer
/// operations such as file dialogs for import/export.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;

    private FretboardWindow? _fretboardWindow;
    private VirtualKeyboardWindow? _keyboardWindow;

    public MainWindow()
    {
        InitializeComponent();

        // Resolve services
        _settingsService = AppHost.GetService<ISettingsService>();

        // Restore saved window bounds
        RestoreWindowBounds();

        // Subscribe via DataContextChanged since DataContext is set AFTER construction
        DataContextChanged += (s, e) =>
        {
            if (e.NewValue is MainViewModel vm)
                vm.PropertyChanged += OnViewModelPropertyChanged;
        };

        Closing += OnWindowClosing;
    }

    // ── Window chrome / drag ────────────────────────────────────────

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Load();
        if (settings.Behavior.CloseToTray)
        {
            Hide();
        }
        else
        {
            Close();
        }
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            var settings = _settingsService.Load();
            if (settings.Behavior.CloseToTray)
            {
                Hide();
            }
        }
    }

    // ── Window bounds persistence ────────────────────────────────────

    private void RestoreWindowBounds()
    {
        var settings = _settingsService.Load();
        if (settings.Appearance.MainWindowWidth > 0 && settings.Appearance.MainWindowHeight > 0)
        {
            Left = settings.Appearance.MainWindowLeft;
            Top = settings.Appearance.MainWindowTop;
            Width = settings.Appearance.MainWindowWidth;
            Height = settings.Appearance.MainWindowHeight;
        }
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // Save window position
        var settings = _settingsService.Load();
        settings.Appearance.MainWindowLeft = (int)Left;
        settings.Appearance.MainWindowTop = (int)Top;
        settings.Appearance.MainWindowWidth = (int)Width;
        settings.Appearance.MainWindowHeight = (int)Height;
        _settingsService.Save(settings);

        // Close child windows
        _fretboardWindow?.Close();
        _keyboardWindow?.Close();
    }

    // ── Sub-window management ───────────────────────────────────────

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsFretboardVisible):
                HandleFretboardToggle(vm);
                break;

            case nameof(MainViewModel.IsKeyboardVisible):
                HandleKeyboardToggle(vm);
                break;
        }
    }

    private void HandleFretboardToggle(MainViewModel vm)
    {
        if (vm.IsFretboardVisible)
        {
            if (_fretboardWindow == null)
            {
                var fretVm = AppHost.GetService<FretboardViewModel>();
                _fretboardWindow = new FretboardWindow { DataContext = fretVm };
                _fretboardWindow.Closed += (s, e) =>
                {
                    _fretboardWindow = null;
                    vm.IsFretboardVisible = false;
                };
            }
            _fretboardWindow.Show();
            _fretboardWindow.Activate();
        }
        else
        {
            _fretboardWindow?.Close();
        }
    }

    private void HandleKeyboardToggle(MainViewModel vm)
    {
        if (vm.IsKeyboardVisible)
        {
            if (_keyboardWindow == null)
            {
                var kbVm = AppHost.GetService<VirtualKeyboardViewModel>();
                _keyboardWindow = new VirtualKeyboardWindow { DataContext = kbVm };
                _keyboardWindow.Closed += (s, e) =>
                {
                    _keyboardWindow = null;
                    vm.IsKeyboardVisible = false;
                };
            }
            _keyboardWindow.Show();
            _keyboardWindow.Activate();
        }
        else
        {
            _keyboardWindow?.Close();
        }
    }

    // ── View-layer command handling (for commands that require UI interaction) ──

    /// <summary>
    /// Opens the MappingEditorDialog for adding a new mapping.
    /// Called in response to AddMappingCommand.
    /// </summary>
    public void ShowAddMappingDialog()
    {
        var editorVm = AppHost.GetService<MappingEditorViewModel>();
        editorVm.InitializeForNew();

        var dialog = new MappingEditorDialog
        {
            DataContext = editorVm,
            Owner = this
        };

        if (dialog.ShowDialog() == true && editorVm.Result != null)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CommitNewMapping(editorVm.Result);
            }
        }
    }

    /// <summary>
    /// Opens the MappingEditorDialog for editing an existing mapping.
    /// Called in response to EditMappingCommand.
    /// </summary>
    public void ShowEditMappingDialog(NoteMapping mapping)
    {
        var editorVm = AppHost.GetService<MappingEditorViewModel>();
        editorVm.InitializeForEdit(mapping);

        var dialog = new MappingEditorDialog
        {
            DataContext = editorVm,
            Owner = this
        };

        if (dialog.ShowDialog() == true && editorVm.Result != null)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CommitEditMapping(editorVm.Result);
            }
        }
    }

    /// <summary>
    /// Opens a file dialog for importing a profile.
    /// Called when ImportProfileCommand is executed.
    /// </summary>
    public void ShowImportProfileDialog()
    {
        var dlg = new OpenFileDialog
        {
            Title = "导入映射方案",
            Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dlg.ShowDialog(this) == true)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.ImportProfileFromFile(dlg.FileName);
            }
        }
    }

    /// <summary>
    /// Opens a save dialog for exporting a profile.
    /// Called when ExportProfileCommand is executed.
    /// </summary>
    public void ShowExportProfileDialog()
    {
        var dlg = new SaveFileDialog
        {
            Title = "导出映射方案",
            Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            DefaultExt = ".json",
            FileName = $"{((MainViewModel?)DataContext)?.SelectedProfile ?? "映射方案"}.json"
        };

        if (dlg.ShowDialog(this) == true)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.ExportProfileToFile(dlg.FileName);
            }
        }
    }

    /// <summary>
    /// Opens the AdvancedSettingsWindow as a modal dialog.
    /// </summary>
    public void ShowAdvancedSettingsDialog()
    {
        var settingsVm = AppHost.GetService<AdvancedSettingsViewModel>();

        var dialog = new AdvancedSettingsWindow
        {
            DataContext = settingsVm,
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.RefreshSettings();
            }
        }
    }
}
