using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GuitarInputController.Core.Enums;

namespace GuitarInputController.UI.Controls;

/// <summary>
/// 虚拟键盘控件 — 显示键盘布局并高亮按键
/// </summary>
public partial class VirtualKeyboardControl : UserControl
{
    #region Dependency Properties

    public static readonly DependencyProperty HighlightedKeysProperty =
        DependencyProperty.Register(nameof(HighlightedKeys), typeof(IEnumerable<string>),
            typeof(VirtualKeyboardControl), new PropertyMetadata(Array.Empty<string>(), OnVisualChanged));

    public static readonly DependencyProperty KeyboardLayoutProperty =
        DependencyProperty.Register(nameof(KeyboardLayout), typeof(KeyboardLayoutType),
            typeof(VirtualKeyboardControl), new PropertyMetadata(KeyboardLayoutType.Key104, OnVisualChanged));

    public IEnumerable<string> HighlightedKeys
    {
        get => (IEnumerable<string>)GetValue(HighlightedKeysProperty);
        set => SetValue(HighlightedKeysProperty, value);
    }

    public KeyboardLayoutType KeyboardLayout
    {
        get => (KeyboardLayoutType)GetValue(KeyboardLayoutProperty);
        set => SetValue(KeyboardLayoutProperty, value);
    }

    #endregion

    // 颜色
    private static readonly Color DefaultKeyColor = Color.FromRgb(0x55, 0x55, 0x55);
    private static readonly Color HighlightKeyColor = Color.FromRgb(0xFF, 0xFF, 0xFF);
    private static readonly Color ModifierKeyColor = Color.FromRgb(0x44, 0x44, 0x44);
    private static readonly Color TextColor = Color.FromRgb(0xDD, 0xDD, 0xDD);
    private static readonly Color HighlightTextColor = Color.FromRgb(0x00, 0x00, 0x00);

    private readonly Dictionary<string, Border> _keyElements = new();

    // 104 键布局定义
    // 每个键: (左, 顶, 宽, 高, 标签, 键码, 是否修饰键)
    private static readonly List<(double L, double T, double W, double H, string Label, string Key, bool Mod)> Key104Layout = new()
    {
        // F 键行
        (0,0,1,1,"Esc","Escape",false), (2,0,1,1,"F1","F1",false), (3,0,1,1,"F2","F2",false),
        (4,0,1,1,"F3","F3",false), (5,0,1,1,"F4","F4",false), (6.5,0,1,1,"F5","F5",false),
        (7.5,0,1,1,"F6","F6",false), (8.5,0,1,1,"F7","F7",false), (9.5,0,1,1,"F8","F8",false),
        (11,0,1,1,"F9","F9",false), (12,0,1,1,"F10","F10",false), (13,0,1,1,"F11","F11",false),
        (14,0,1,1,"F12","F12",false), (15.5,0,1,1,"PrtSc","PrintScreen",false),
        (16.5,0,1,1,"Scrl","ScrollLock",false), (17.5,0,1,1,"Pause","Pause",false),

        // 数字行
        (0,1.5,1,1,"~\n`","`",false), (1,1.5,1,1,"!\n1","1",false), (2,1.5,1,1,"@\n2","2",false),
        (3,1.5,1,1,"#\n3","3",false), (4,1.5,1,1,"$\n4","4",false), (5,1.5,1,1,"%\n5","5",false),
        (6,1.5,1,1,"^\n6","6",false), (7,1.5,1,1,"&\n7","7",false), (8,1.5,1,1,"*\n8","8",false),
        (9,1.5,1,1,"(\n9","9",false), (10,1.5,1,1,")\n0","0",false), (11,1.5,1,1,"_\n-","-",false),
        (12,1.5,1,1,"+\n=","=",false), (13,1.5,2,1,"Backspace","Backspace",true),

        // Tab 行
        (0,2.5,1.5,1,"Tab","Tab",true), (1.5,2.5,1,1,"Q","Q",false), (2.5,2.5,1,1,"W","W",false),
        (3.5,2.5,1,1,"E","E",false), (4.5,2.5,1,1,"R","R",false), (5.5,2.5,1,1,"T","T",false),
        (6.5,2.5,1,1,"Y","Y",false), (7.5,2.5,1,1,"U","U",false), (8.5,2.5,1,1,"I","I",false),
        (9.5,2.5,1,1,"O","O",false), (10.5,2.5,1,1,"P","P",false), (11.5,2.5,1,1,"{\n[","[",false),
        (12.5,2.5,1,1,"}\n]","]",false), (13.5,2.5,1.5,1,"|\n\\","\\",false),

        // Caps 行
        (0,3.5,1.75,1,"Caps","CapsLock",true), (1.75,3.5,1,1,"A","A",false),
        (2.75,3.5,1,1,"S","S",false), (3.75,3.5,1,1,"D","D",false),
        (4.75,3.5,1,1,"F","F",false), (5.75,3.5,1,1,"G","G",false),
        (6.75,3.5,1,1,"H","H",false), (7.75,3.5,1,1,"J","J",false),
        (8.75,3.5,1,1,"K","K",false), (9.75,3.5,1,1,"L","L",false),
        (10.75,3.5,1,1,":\n;",";",false), (11.75,3.5,1,1,"\"\n'","'",false),
        (12.75,3.5,2.25,1,"Enter","Enter",true),

        // Shift 行
        (0,4.5,2.25,1,"Shift","Shift",true), (2.25,4.5,1,1,"Z","Z",false),
        (3.25,4.5,1,1,"X","X",false), (4.25,4.5,1,1,"C","C",false),
        (5.25,4.5,1,1,"V","V",false), (6.25,4.5,1,1,"B","B",false),
        (7.25,4.5,1,1,"N","N",false), (8.25,4.5,1,1,"M","M",false),
        (9.25,4.5,1,1,"<\n,",",",false), (10.25,4.5,1,1,">\n.",".",false),
        (11.25,4.5,1,1,"?\n/","/",false), (12.25,4.5,2.75,1,"Shift","Shift",true),

        // Ctrl 行
        (0,5.5,1.5,1,"Ctrl","Ctrl",true), (1.5,5.5,1.25,1,"Win","Win",true),
        (2.75,5.5,1.25,1,"Alt","Alt",true), (4,5.5,7,1,"Space","Space",false),
        (11,5.5,1.25,1,"Alt","Alt",true), (12.25,5.5,1.25,1,"Win","Win",true),
        (13.5,5.5,1.25,1,"Menu","Menu",true), (14.75,5.5,1.25,1,"Ctrl","Ctrl",true),

        // 方向键
        (16.5,4.5,1,1,"▲","Up",false),
        (15.5,5.5,1,1,"◄","Left",false), (16.5,5.5,1,1,"▼","Down",false), (17.5,5.5,1,1,"►","Right",false),
    };

    public VirtualKeyboardControl()
    {
        InitializeComponent();
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VirtualKeyboardControl control)
            control.Redraw();
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Redraw();
    }

    private void Redraw()
    {
        KeyboardCanvas.Children.Clear();
        _keyElements.Clear();

        double canvasWidth = KeyboardCanvas.ActualWidth;
        if (canvasWidth <= 0) return;

        // 选择布局
        var layout = GetCurrentLayout();
        if (layout.Count == 0) return;

        // 计算布局参数
        double maxRight = layout.Max(k => k.L + k.W);
        double unitWidth = canvasWidth / (maxRight + 0.5);
        double keyHeight = Math.Min(40, KeyboardCanvas.ActualHeight / 7);

        var highlightedSet = new HashSet<string>(
            (HighlightedKeys ?? Array.Empty<string>()).Select(k => k.ToUpperInvariant()),
            StringComparer.OrdinalIgnoreCase);

        foreach (var (left, top, width, height, label, keyCode, isMod) in layout)
        {
            double x = left * unitWidth + 4;
            double y = top * (keyHeight + 6) + 4;
            double w = width * unitWidth - 8;
            double h = keyHeight - 4;

            bool isHighlighted = highlightedSet.Contains(keyCode.ToUpperInvariant());

            var border = new Border
            {
                Width = w,
                Height = h,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(isHighlighted ? HighlightKeyColor :
                    (isMod ? ModifierKeyColor : DefaultKeyColor)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                BorderThickness = new Thickness(1)
            };

            var textBlock = new TextBlock
            {
                Text = label.Replace("\n", " "),
                FontSize = w > 30 ? 11 : 9,
                Foreground = new SolidColorBrush(isHighlighted ? HighlightTextColor : TextColor),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            border.Child = textBlock;

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);
            KeyboardCanvas.Children.Add(border);
            _keyElements[keyCode] = border;
        }
    }

    private List<(double L, double T, double W, double H, string Label, string Key, bool Mod)> GetCurrentLayout()
    {
        return KeyboardLayout switch
        {
            KeyboardLayoutType.Key104 => Key104Layout,
            KeyboardLayoutType.Key87 => Get87KeyLayout(),
            KeyboardLayoutType.Key98 => Get98KeyLayout(),
            KeyboardLayoutType.Key68 => Get68KeyLayout(),
            _ => Key104Layout
        };
    }

    /// <summary>87 键布局 = 104 键去掉数字键盘区</summary>
    private static List<(double, double, double, double, string, string, bool)> Get87KeyLayout()
    {
        return Key104Layout.Where(k => k.L < 15.5 || k.Key is "Up" or "Left" or "Down" or "Right").ToList();
    }

    /// <summary>98 键布局 = 紧凑布局（简化为全键的排列）</summary>
    private static List<(double, double, double, double, string, string, bool)> Get98KeyLayout()
    {
        // 简化实现：保留主要区域
        return Key104Layout.Where(k => k.L < 16).ToList();
    }

    /// <summary>68 键布局 = 迷你布局</summary>
    private static List<(double, double, double, double, string, string, bool)> Get68KeyLayout()
    {
        // 简化实现：只保留主输入区域和部分功能键
        return Key104Layout.Where(k => k.L < 15 && k.T > 1 && k.T < 6).ToList();
    }
}
