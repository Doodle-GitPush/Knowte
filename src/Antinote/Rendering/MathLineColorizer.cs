using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Antinote.Rendering;

public class MathLineColorizer : DocumentColorizingTransformer
{
    private static readonly Brush PrefixBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));

    protected override void ColorizeLine(DocumentLine line)
    {
        var text = CurrentContext.Document.GetText(line);
        if (!text.StartsWith("= ")) return;

        ChangeLinePart(line.Offset, line.Offset + 2, element =>
            element.TextRunProperties.SetForegroundBrush(PrefixBrush));
    }
}
