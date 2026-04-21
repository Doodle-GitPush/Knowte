using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Antinote.Rendering;

public class ModeTokenColorizer : DocumentColorizingTransformer
{
    private static readonly Dictionary<string, Color> ModeColors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "math",      Color.FromRgb(0x7C, 0x3A, 0xED) }, // #7C3AED purple
        { "list",      Color.FromRgb(0x05, 0x96, 0x69) }, // #059669 green
        { "checklist", Color.FromRgb(0x05, 0x96, 0x69) }, // #059669 green
        { "convert",   Color.FromRgb(0x02, 0x84, 0xC7) }, // #0284C7 blue
        { "paste",     Color.FromRgb(0xD9, 0x77, 0x06) }, // #D97706 amber
        { "timer",     Color.FromRgb(0xF9, 0x73, 0x16) }, // #F97316 orange
    };

    private static readonly Brush SemicolonBrush =
        new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));

    protected override void ColorizeLine(DocumentLine line)
    {
        if (line.LineNumber != 1) return;

        var text = CurrentContext.Document.GetText(line);

        // Walk the text character by character to find tokens and semicolons
        int i = 0;
        while (i < text.Length)
        {
            // Skip whitespace
            if (char.IsWhiteSpace(text[i])) { i++; continue; }

            // Semicolon as separator
            if (text[i] == ';')
            {
                int semiStart = line.Offset + i;
                ChangeLinePart(semiStart, semiStart + 1, el =>
                    el.TextRunProperties.SetForegroundBrush(SemicolonBrush));
                i++;
                continue;
            }

            // Read a token (until whitespace or semicolon)
            int tokenStart = i;
            while (i < text.Length && text[i] != ';' && !char.IsWhiteSpace(text[i]))
                i++;
            int tokenEnd = i;

            var token = text.Substring(tokenStart, tokenEnd - tokenStart);

            if (ModeColors.TryGetValue(token, out var color))
            {
                int absStart = line.Offset + tokenStart;
                int absEnd = line.Offset + tokenEnd;
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                ChangeLinePart(absStart, absEnd, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(brush);
                });
            }
        }
    }
}
