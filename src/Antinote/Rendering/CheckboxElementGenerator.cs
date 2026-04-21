using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Antinote.Rendering;

public class CheckboxElementGenerator : VisualLineElementGenerator
{
    private const string Unchecked = "- [ ] ";
    private const string Checked   = "- [x] ";
    private readonly TextDocument _document;

    public CheckboxElementGenerator(TextDocument document)
    {
        _document = document;
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        var text = _document.Text;
        var pos = startOffset;

        while (pos < text.Length)
        {
            bool atLineStart = pos == 0 || text[pos - 1] == '\n';
            if (atLineStart && pos + 6 <= text.Length)
            {
                var seg = text.Substring(pos, 6);
                if (seg == Unchecked || seg == Checked)
                    return pos;
            }
            var next = text.IndexOf('\n', pos);
            if (next == -1) break;
            pos = next + 1;
        }
        return -1;
    }

    public override VisualLineElement ConstructElement(int offset)
    {
        if (offset + 6 > _document.TextLength) return null!;
        var seg = _document.GetText(offset, 6);
        if (seg != Unchecked && seg != Checked) return null!;

        bool isChecked = seg == Checked;
        var control = new CheckboxControl(isChecked, () => Toggle(offset));
        return new CheckboxVisualElement(control);
    }

    private void Toggle(int offset)
    {
        if (offset + 6 > _document.TextLength) return;
        var current = _document.GetText(offset, 6);
        _document.Replace(offset, 6, current == Unchecked ? Checked : Unchecked);
    }
}

internal class CheckboxVisualElement : VisualLineElement
{
    private readonly UIElement _element;

    public CheckboxVisualElement(UIElement element) : base(1, 6)
    {
        _element = element;
    }

    public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        => new InlineObjectRun(1, context.GlobalTextRunProperties, _element);
}

internal class CheckboxControl : FrameworkElement
{
    private readonly bool _isChecked;
    private double _checkProgress;

    private static readonly Pen BorderPen = new(new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), 1.5)
        { LineJoin = PenLineJoin.Round };
    private static readonly Brush CheckedBg = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
    private static readonly Pen CheckPen = new(Brushes.White, 1.5)
        { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };

    static CheckboxControl()
    {
        BorderPen.Freeze();
        CheckedBg.Freeze();
        CheckPen.Freeze();
    }

    public CheckboxControl(bool isChecked, Action toggle)
    {
        _isChecked = isChecked;
        _checkProgress = isChecked ? 1.0 : 0.0;
        Width = 16;
        Height = 22;   // taller than the circle to create line spacing
        Cursor = Cursors.Hand;
        Margin = new Thickness(0, 0, 6, 0);
        // Tell AvalonEdit where the text baseline sits within this element
        // so the circle center (at y=11) aligns with text cap-center (~3.5px above baseline)
        System.Windows.Controls.TextBlock.SetBaselineOffset(this, 14.5);

        MouseLeftButtonDown += (_, e) =>
        {
            toggle();
            e.Handled = true;
        };

        if (isChecked)
            AnimateCheckIn();
    }

    private void AnimateCheckIn()
    {
        _checkProgress = 0;
        var timer = new System.Windows.Threading.DispatcherTimer
            { Interval = TimeSpan.FromMilliseconds(16) };
        double elapsed = 0;
        timer.Tick += (_, _) =>
        {
            elapsed += 16;
            _checkProgress = Math.Min(1.0, elapsed / 120.0);
            InvalidateVisual();
            if (elapsed >= 120) timer.Stop();
        };
        timer.Start();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var rect = new Rect(0, 3, 16, 16);  // circle centered at y=11 within 22px height

        if (_isChecked)
        {
            dc.DrawRoundedRectangle(CheckedBg, null, rect, 7, 7);

            if (_checkProgress > 0)
            {
                // Two-segment checkmark: scaled for 14px circle
                const double seg1End = 0.34;
                var p0 = new Point(3.5, 11);
                var p1 = new Point(6.5, 14);
                var p2 = new Point(11.5, 7.5);

                if (_checkProgress <= seg1End)
                {
                    double t = _checkProgress / seg1End;
                    dc.DrawLine(CheckPen, p0, Lerp(p0, p1, t));
                }
                else
                {
                    dc.DrawLine(CheckPen, p0, p1);
                    double t = (_checkProgress - seg1End) / (1.0 - seg1End);
                    dc.DrawLine(CheckPen, p1, Lerp(p1, p2, t));
                }
            }
        }
        else
        {
            dc.DrawRoundedRectangle(Brushes.White, BorderPen, rect, 7, 7);
        }
    }

    private static Point Lerp(Point a, Point b, double t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}
