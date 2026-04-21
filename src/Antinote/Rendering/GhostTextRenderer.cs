using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;

namespace Knowte.Rendering;

public class GhostTextRenderer : IBackgroundRenderer
{
    private readonly TextEditor _editor;
    private string? _ghostText;
    private double _opacity;
    private DispatcherTimer? _fadeTimer;

    public KnownLayer Layer => KnownLayer.Background;

    public GhostTextRenderer(TextEditor editor)
    {
        _editor = editor;
    }

    public string? GhostText => _ghostText;

    public AcceptedGhostColorizer? AcceptedColorizer { get; set; }

    public void SetGhost(string? text)
    {
        if (text == _ghostText) return;
        _ghostText = text;
        _fadeTimer?.Stop();

        if (text != null)
        {
            _opacity = 0;
            double elapsed = 0;
            _fadeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _fadeTimer.Tick += (_, _) =>
            {
                elapsed += 16;
                _opacity = Math.Min(1.0, elapsed / 150.0);
                _editor.TextArea.TextView.InvalidateLayer(Layer);
                if (elapsed >= 150) _fadeTimer!.Stop();
            };
            _fadeTimer.Start();
        }
        else
        {
            _opacity = 0;
            _editor.TextArea.TextView.InvalidateLayer(Layer);
        }
    }

    public void Accept()
    {
        if (_ghostText == null) return;
        var text = _ghostText;
        int caretOffset = _editor.CaretOffset;
        SetGhost(null);
        _editor.Document.Insert(caretOffset, text);
        AcceptedColorizer?.MarkAccepted(caretOffset, text.Length);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_ghostText == null || _opacity <= 0) return;
        if (!textView.VisualLinesValid) return;

        try
        {
            var caretOffset = _editor.CaretOffset;
            if (caretOffset > _editor.Document.TextLength) return;

            var docLine = _editor.Document.GetLineByOffset(caretOffset);

            VisualLine? vLine = null;
            foreach (var vl in textView.VisualLines)
            {
                if (vl.FirstDocumentLine.LineNumber == docLine.LineNumber)
                { vLine = vl; break; }
            }
            if (vLine == null) return;

            var pos = textView.GetVisualPosition(
                new ICSharpCode.AvalonEdit.TextViewPosition(docLine.LineNumber, docLine.Length + 1),
                VisualYPosition.TextTop);

            var alpha = (byte)(_opacity * 200);
            var brush = new SolidColorBrush(Color.FromArgb(alpha, 0xCC, 0xCC, 0xCC));
            brush.Freeze();

            var typeface = new Typeface(_editor.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var pixelsPerDip = VisualTreeHelper.GetDpi(textView).PixelsPerDip;

            var ft = new FormattedText(
                _ghostText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                _editor.FontSize,
                brush,
                pixelsPerDip);

            drawingContext.DrawText(ft, new Point(pos.X, pos.Y));
        }
        catch
        {
            // Suppress rendering errors (e.g. visual lines not yet built)
        }
    }
}
