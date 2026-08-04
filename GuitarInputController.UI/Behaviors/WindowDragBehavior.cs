using System.Windows;
using System.Windows.Input;

namespace GuitarInputController.UI.Behaviors;

/// <summary>
/// 无边框窗口拖拽行为 — 附加属性，使窗口可在任意位置拖拽移动
/// 用法: WindowDragBehavior.IsDragable="True" 在需要拖拽的元素上
/// </summary>
public static class WindowDragBehavior
{
    public static readonly DependencyProperty IsDragableProperty =
        DependencyProperty.RegisterAttached(
            "IsDragable",
            typeof(bool),
            typeof(WindowDragBehavior),
            new PropertyMetadata(false, OnIsDragableChanged));

    public static bool GetIsDragable(DependencyObject obj) =>
        (bool)obj.GetValue(IsDragableProperty);

    public static void SetIsDragable(DependencyObject obj, bool value) =>
        obj.SetValue(IsDragableProperty, value);

    private static void OnIsDragableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if ((bool)e.NewValue)
                element.MouseLeftButtonDown += OnMouseLeftButtonDown;
            else
                element.MouseLeftButtonDown -= OnMouseLeftButtonDown;
        }
    }

    private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DependencyObject dep)
        {
            var window = Window.GetWindow(dep);
            if (window != null)
            {
                window.DragMove();
            }
        }
    }
}
