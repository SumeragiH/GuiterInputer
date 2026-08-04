using System.Windows;
using System.Windows.Forms; // System.Windows.Forms for NotifyIcon

namespace GuitarInputController.App.Services;

/// <summary>
/// 系统托盘管理器
/// </summary>
public class TrayIconManager : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private readonly Action _showMainWindow;
    private readonly Action _toggleInput;
    private readonly Action _exitApplication;

    public TrayIconManager(Action showMainWindow, Action toggleInput, Action exitApplication)
    {
        _showMainWindow = showMainWindow;
        _toggleInput = toggleInput;
        _exitApplication = exitApplication;
    }

    public void Initialize()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "电吉他输入控制器"
        };

        var contextMenu = new ContextMenuStrip();

        contextMenu.Items.Add("显示主窗口", null, (s, e) => _showMainWindow());
        contextMenu.Items.Add("启用/禁用吉他输入", null, (s, e) => _toggleInput());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("退出", null, (s, e) => _exitApplication());

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => _showMainWindow();
    }

    public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon?.ShowBalloonTip(3000, title, text, icon);
    }

    public void SetToolTip(string text)
    {
        if (_notifyIcon != null)
            _notifyIcon.Text = text;
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
    }
}
