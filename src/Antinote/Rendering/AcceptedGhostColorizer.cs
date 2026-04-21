using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Antinote.Rendering;

public class AcceptedGhostColorizer : DocumentColorizingTransformer
{
    private int _startOffset = -1;
    private int _length = 0;
    private DateTime _acceptedAt = DateTime.MinValue;
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(800);

    public void MarkAccepted(int startOffset, int length)
    {
        _startOffset = startOffset;
        _length = length;
        _acceptedAt = DateTime.Now;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (_startOffset < 0 || _length <= 0) return;
        var elapsed = DateTime.Now - _acceptedAt;
        if (elapsed > Duration)
        {
            _startOffset = -1;
            return;
        }

        double progress = elapsed.TotalMilliseconds / Duration.TotalMilliseconds;
        byte alpha = (byte)(180 * (1.0 - progress)); // fade from 180 → 0

        int lineStart = line.Offset;
        int lineEnd = line.EndOffset;
        int markEnd = _startOffset + _length;

        int start = Math.Max(_startOffset, lineStart);
        int end = Math.Min(markEnd, lineEnd);
        if (start >= end) return;

        ChangeLinePart(start, end, el =>
        {
            el.TextRunProperties.SetForegroundBrush(
                new SolidColorBrush(Color.FromArgb(alpha, 0xF9, 0x73, 0x16)));
        });

        // Request redraw to keep fading
        CurrentContext.TextView.InvalidateLayer(KnownLayer.Text);
    }
}
