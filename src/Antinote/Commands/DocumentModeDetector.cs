namespace Antinote.Commands;

public enum DocumentMode { None, Math, List, Checklist, Convert, Paste }

public static class DocumentModeDetector
{
    public static DocumentMode Detect(string documentText)
    {
        if (string.IsNullOrWhiteSpace(documentText)) return DocumentMode.None;
        var firstLine = documentText.Split('\n')[0].Trim();
        if (string.Equals(firstLine, "math",      StringComparison.OrdinalIgnoreCase)) return DocumentMode.Math;
        if (string.Equals(firstLine, "list",      StringComparison.OrdinalIgnoreCase)) return DocumentMode.List;
        if (string.Equals(firstLine, "checklist", StringComparison.OrdinalIgnoreCase)) return DocumentMode.Checklist;
        if (string.Equals(firstLine, "convert",   StringComparison.OrdinalIgnoreCase)) return DocumentMode.Convert;
        if (string.Equals(firstLine, "paste",     StringComparison.OrdinalIgnoreCase)) return DocumentMode.Paste;
        return DocumentMode.None;
    }
}
