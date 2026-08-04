using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using GuitarInputController.App.Services;
using GuitarInputController.App.ViewModels;
using GuitarInputController.App.Views;
using GuitarInputController.Core.Services;

namespace GuitarInputController.App;

/// <summary>
/// Application entry point. Configures the DI container, loads settings,
/// manages the tray icon, and handles application lifecycle.
/// </summary>
public partial class App : System.Windows.Application
{
    private TrayIconManager? _trayManager;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Configure dependency injection
            AppHost.Configure();

            // Load persisted settings
            var settingsService = AppHost.GetService<ISettingsService>();
            var settings = settingsService.Load();

            // Initialize system tray
            _trayManager = new TrayIconManager(
                showMainWindow: ShowMainWindow,
                toggleInput: ToggleInput,
                exitApplication: ShutdownApplication);
            _trayManager.Initialize();

            // Show the main window (or start minimized to tray)
            if (!settings.Behavior.StartMinimized)
            {
                ShowMainWindow();
            }
            else
            {
                _trayManager.ShowBalloonTip(
                    "电吉他输入控制器",
                    "程序已在系统托盘启动，双击图标打开主窗口。");
            }
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                msg += $"\n→ {inner.Message}";
                inner = inner.InnerException;
            }
            System.Windows.MessageBox.Show(
                $"启动失败:\n\n{msg}\n\n堆栈:\n{ex.StackTrace}",
                "启动错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    /// <summary>
    /// Creates or brings the main window into view.
    /// </summary>
    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            var viewModel = AppHost.GetService<MainViewModel>();
            _mainWindow = new MainWindow { DataContext = viewModel };
            _mainWindow.Closed += (s, e) => _mainWindow = null;
        }

        _mainWindow.Show();
        _mainWindow.Activate();
        if (_mainWindow.WindowState == WindowState.Minimized)
            _mainWindow.WindowState = WindowState.Normal;
    }

    /// <summary>
    /// Toggles audio capture on/off from the tray menu.
    /// </summary>
    private void ToggleInput()
    {
        if (_mainWindow?.DataContext is MainViewModel vm)
        {
            if (vm.IsCapturing)
                vm.StopCaptureCommand.Execute(null);
            else
                vm.StartCaptureCommand.Execute(null);
        }
    }

    /// <summary>
    /// Gracefully shuts down the application.
    /// </summary>
    private void ShutdownApplication()
    {
        _trayManager?.Dispose();
        Shutdown();
    }

    /// <summary>
    /// Global unhandled exception handler — logs the error and prevents a crash
    /// when possible.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var msg = e.Exception.Message;
        var inner = e.Exception.InnerException;
        while (inner != null)
        {
            msg += $"\n→ {inner.Message}";
            inner = inner.InnerException;
        }
        Debug.WriteLine($"[FATAL] Unhandled exception: {e.Exception}");
        System.Windows.MessageBox.Show(
            $"发生未处理的错误:\n\n{msg}",
            "错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayManager?.Dispose();
        base.OnExit(e);
    }
}
