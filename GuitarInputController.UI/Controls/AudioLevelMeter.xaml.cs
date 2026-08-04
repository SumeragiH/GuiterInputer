using System.Windows;
using System.Windows.Controls;

namespace GuitarInputController.UI.Controls;

/// <summary>
/// 音频电平指示器
/// </summary>
public partial class AudioLevelMeter : UserControl
{
    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(nameof(Level), typeof(double),
            typeof(AudioLevelMeter), new PropertyMetadata(0.0, OnLevelChanged));

    public double Level
    {
        get => (double)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public AudioLevelMeter()
    {
        InitializeComponent();
    }

    private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioLevelMeter meter)
        {
            // 确保在 UI 线程上更新控件，避免跨线程访问异常
            meter.Dispatcher.InvokeAsync(() =>
            {
                meter.LevelBar.Value = (double)e.NewValue;
            });
        }
    }
}
