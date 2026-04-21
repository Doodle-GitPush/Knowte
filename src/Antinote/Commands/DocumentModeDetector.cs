namespace Antinote.Commands;

[Flags]
public enum DocumentMode
{
    None      = 0,
    Math      = 1,
    List      = 2,
    Checklist = 4,
    Convert   = 8,
    Paste     = 16,
    Timer     = 32
}

public static class DocumentModeDetector
{
    private static readonly HashSet<string> ModeNames =
        new(StringComparer.OrdinalIgnoreCase) { "math", "list", "checklist", "convert", "paste", "timer" };

    public static bool IsModeName(string word) => ModeNames.Contains(word);

    public static DocumentMode Detect(string documentText)
    {
        if (string.IsNullOrWhiteSpace(documentText)) return DocumentMode.None;
        var firstLine = documentText.Split('\n')[0].Trim();

        // Multi-mode: tokens on line 1 that end with ';' activate their mode
        if (firstLine.Contains(';'))
        {
            var result = DocumentMode.None;
            foreach (var token in firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!token.EndsWith(';')) continue;
                result |= ParseMode(token[..^1]);
            }
            return result;
        }

        // Backward-compat: single bare word on line 1
        return ParseMode(firstLine);
    }

    private static DocumentMode ParseMode(string word) =>
        word.ToLowerInvariant() switch
        {
            "math"      => DocumentMode.Math,
            "list"      => DocumentMode.List,
            "checklist" => DocumentMode.Checklist,
            "convert"   => DocumentMode.Convert,
            "paste"     => DocumentMode.Paste,
            "timer"     => DocumentMode.Timer,
            _           => DocumentMode.None
        };
}
