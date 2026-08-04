using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GuitarInputController.Core.Extensions;
using GuitarInputController.Core.Models;

namespace GuitarInputController.UI.Controls;

/// <summary>
/// 吉他指板控件 — 可视化显示吉他指板并高亮当前音符位置
/// </summary>
public partial class FretboardControl : UserControl
{
    #region Dependency Properties

    public static readonly DependencyProperty HighlightedNotesProperty =
        DependencyProperty.Register(nameof(HighlightedNotes), typeof(IEnumerable<string>),
            typeof(FretboardControl), new PropertyMetadata(Array.Empty<string>(), OnVisualChanged));

    public static readonly DependencyProperty StringCountProperty =
        DependencyProperty.Register(nameof(StringCount), typeof(int),
            typeof(FretboardControl), new PropertyMetadata(6, OnVisualChanged));

    public static readonly DependencyProperty FretCountProperty =
        DependencyProperty.Register(nameof(FretCount), typeof(int),
            typeof(FretboardControl), new PropertyMetadata(24, OnVisualChanged));

    public static readonly DependencyProperty BackgroundColorProperty =
        DependencyProperty.Register(nameof(BackgroundColor), typeof(Brush),
            typeof(FretboardControl), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)), OnVisualChanged));

    public IEnumerable<string> HighlightedNotes
    {
        get => (IEnumerable<string>)GetValue(HighlightedNotesProperty);
        set => SetValue(HighlightedNotesProperty, value);
    }

    public int StringCount
    {
        get => (int)GetValue(StringCountProperty);
        set => SetValue(StringCountProperty, value);
    }

    public int FretCount
    {
        get => (int)GetValue(FretCountProperty);
        set => SetValue(FretCountProperty, value);
    }

    public Brush BackgroundColor
    {
        get => (Brush)GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    #endregion

    private static readonly Brush StringBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
    private static readonly Brush FretBrush = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
    private static readonly Brush FretMarkerBrush = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
    private static readonly Brush HighlightBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xAA, 0xFF));
    private static readonly Brush OpenStringBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
    private static readonly Typeface LabelTypeface = new("Segoe UI");

    private readonly List<Visual> _highlightVisuals = new();

    public FretboardControl()
    {
        InitializeComponent();
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FretboardControl control)
            control.Redraw();
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Redraw();
    }

    private void Redraw()
    {
        FretboardCanvas.Children.Clear();
        _highlightVisuals.Clear();

        double width = FretboardCanvas.ActualWidth;
        double height = FretboardCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        int strings = StringCount;
        int frets = FretCount;

        // 布局参数
        double leftMargin = 50;
        double rightMargin = 20;
        double topMargin = 15;
        double bottomMargin = 15;

        double fretboardLeft = leftMargin;
        double fretboardRight = width - rightMargin;
        double fretboardWidth = fretboardRight - fretboardLeft;
        double fretSpacing = fretboardWidth / (frets + 1);
        double stringSpacing = (height - topMargin - bottomMargin) / (strings - 1);

        // 绘制品丝和品位标记
        for (int f = 0; f <= frets; f++)
        {
            double x = fretboardLeft + f * fretSpacing;

            // 品丝线
            var fretLine = new Line
            {
                X1 = x, Y1 = topMargin,
                X2 = x, Y2 = height - bottomMargin,
                Stroke = FretBrush,
                StrokeThickness = f == 0 ? 3 : 1
            };
            FretboardCanvas.Children.Add(fretLine);

            // 品位标记点
            if (ShouldShowFretMarker(f, frets))
            {
                double markerY = height / 2;
                var marker = new Ellipse
                {
                    Width = 8, Height = 8,
                    Fill = FretMarkerBrush
                };
                Canvas.SetLeft(marker, x + fretSpacing / 2 - 4);
                Canvas.SetTop(marker, markerY - 4);
                FretboardCanvas.Children.Add(marker);
            }

            // 双点标记（12 品、24 品）
            if (f == 12 || f == 24)
            {
                double upperY = height * 0.33;
                double lowerY = height * 0.67;
                foreach (var y in new[] { upperY, lowerY })
                {
                    var marker = new Ellipse
                    {
                        Width = 8, Height = 8,
                        Fill = FretMarkerBrush
                    };
                    Canvas.SetLeft(marker, x + fretSpacing / 2 - 4);
                    Canvas.SetTop(marker, y - 4);
                    FretboardCanvas.Children.Add(marker);
                }
            }
        }

        // 品数字标签
        for (int f = 1; f <= frets; f++)
        {
            double x = fretboardLeft + (f - 0.5) * fretSpacing;
            var text = DrawText(f.ToString(), x, height - bottomMargin + 13, 9, FretBrush, HorizontalAlignment.Center);
            FretboardCanvas.Children.Add(text);
        }

        // 绘制弦
        for (int s = 0; s < strings; s++)
        {
            double y = topMargin + s * stringSpacing;
            var stringLine = new Line
            {
                X1 = fretboardLeft, Y1 = y,
                X2 = fretboardRight, Y2 = y,
                Stroke = StringBrush,
                StrokeThickness = 1 + (strings - 1 - s) * 0.4 // 粗弦更粗
            };
            FretboardCanvas.Children.Add(stringLine);

            // 开放弦标签
            var (_, noteName, octave) = Core.Constants.NoteConstants.StandardTuning[s];
            var label = DrawText($"{noteName}{octave}", leftMargin - 10, y, 11, OpenStringBrush, HorizontalAlignment.Right);
            FretboardCanvas.Children.Add(label);
        }

        // 高亮音符位置
        DrawHighlights(strings, frets, fretboardLeft, topMargin, fretSpacing, stringSpacing);
    }

    private void DrawHighlights(int strings, int frets, double left, double top,
        double fretSpacing, double stringSpacing)
    {
        var notes = HighlightedNotes?.ToList() ?? new List<string>();
        if (notes.Count == 0) return;

        foreach (var noteName in notes)
        {
            // 查找该音符在指板上的所有位置
            for (int s = 0; s < strings; s++)
            {
                for (int f = 0; f <= frets; f++)
                {
                    var note = NoteExtensions.GetNoteAtFret(s, f);
                    if (note.FullName == noteName)
                    {
                        double x = left + f * fretSpacing;
                        double y = top + s * stringSpacing;
                        double markerSize = 16;

                        var highlight = new Ellipse
                        {
                            Width = markerSize,
                            Height = markerSize,
                            Fill = HighlightBrush,
                            Opacity = 0.9
                        };
                        Canvas.SetLeft(highlight, x - markerSize / 2);
                        Canvas.SetTop(highlight, y - markerSize / 2);
                        FretboardCanvas.Children.Add(highlight);
                        _highlightVisuals.Add(highlight);
                    }
                }
            }
        }
    }

    private static TextBlock DrawText(string text, double x, double y, double fontSize, Brush brush,
        HorizontalAlignment hAlign = HorizontalAlignment.Left)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            Foreground = brush,
            FontFamily = new FontFamily("Segoe UI")
        };

        // 使用 Measure 来计算文本宽度以实现居中
        tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double textWidth = tb.DesiredSize.Width;

        double left = hAlign == HorizontalAlignment.Center ? x - textWidth / 2 : x;
        Canvas.SetLeft(tb, left);
        Canvas.SetTop(tb, y - fontSize / 2);

        return tb;
    }

    private static bool ShouldShowFretMarker(int fret, int totalFrets)
    {
        if (fret == 0 || fret > totalFrets) return false;
        // 3, 5, 7, 9, 12, 15, 17, 19, 21, 24
        return fret % 12 == 3 || fret % 12 == 5 || fret % 12 == 7 ||
               (fret % 12 == 9 && fret <= totalFrets) || fret % 12 == 0;
    }
}
