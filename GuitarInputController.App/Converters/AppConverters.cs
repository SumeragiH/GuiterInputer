using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GuitarInputController.App.Converters;

/// <summary>
/// Bool 转 Visibility（true=Visible, false=Collapsed）
/// 支持 ConverterParameter="Invert" 反转
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is true;
        if (parameter is string s && s == "Invert")
            boolValue = !boolValue;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

/// <summary>
/// 采集状态转颜色（capturing=绿色, stopped=灰色）
/// </summary>
public class CapturingToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush GreenBrush = new(System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));
    private static readonly SolidColorBrush GrayBrush = new(System.Windows.Media.Color.FromRgb(0xCC, 0xCC, 0xCC));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? GreenBrush : GrayBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
