using System.Windows;

namespace GuitarInputController.UI.Behaviors;

/// <summary>
/// 窗口贴边吸附行为
/// </summary>
public static class WindowSnapBehavior
{
    public static readonly DependencyProperty IsSnapEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsSnapEnabled",
            typeof(bool),
            typeof(WindowSnapBehavior),
            new PropertyMetadata(false, OnIsSnapEnabledChanged));

    public static readonly DependencyProperty SnapDistanceProperty =
        DependencyProperty.RegisterAttached(
            "SnapDistance",
            typeof(double),
            typeof(WindowSnapBehavior),
            new PropertyMetadata(20.0));

    public static bool GetIsSnapEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsSnapEnabledProperty);

    public static void SetIsSnapEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsSnapEnabledProperty, value);

    public static double GetSnapDistance(DependencyObject obj) =>
        (double)obj.GetValue(SnapDistanceProperty);

    public static void SetSnapDistance(DependencyObject obj, double value) =>
        obj.SetValue(SnapDistanceProperty, value);

    private static void OnIsSnapEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window)
        {
            if ((bool)e.NewValue)
                window.LocationChanged += OnWindowLocationChanged;
            else
                window.LocationChanged -= OnWindowLocationChanged;
        }
    }

    private static void OnWindowLocationChanged(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;
        if (!GetIsSnapEnabled(window)) return;

        double snapDistance = GetSnapDistance(window);
        SnapToScreenEdges(window, snapDistance);
    }

    private static void SnapToScreenEdges(Window window, double snapDistance)
    {
        // 使用 WPF 的虚拟屏幕边界（覆盖所有显示器）
        double screenLeft = SystemParameters.VirtualScreenLeft;
        double screenTop = SystemParameters.VirtualScreenTop;
        double screenRight = screenLeft + SystemParameters.VirtualScreenWidth;
        double screenBottom = screenTop + SystemParameters.VirtualScreenHeight;

        // 左边缘吸附
        if (Math.Abs(window.Left - screenLeft) < snapDistance)
            window.Left = screenLeft;
        // 右边缘吸附
        if (Math.Abs(window.Left + window.ActualWidth - screenRight) < snapDistance)
            window.Left = screenRight - window.ActualWidth;
        // 上边缘吸附
        if (Math.Abs(window.Top - screenTop) < snapDistance)
            window.Top = screenTop;
        // 下边缘吸附
        if (Math.Abs(window.Top + window.ActualHeight - screenBottom) < snapDistance)
            window.Top = screenBottom - window.ActualHeight;
    }
}
