namespace Antinote.Commands;

public static class MathModeDetector
{
    public static bool IsActive(string documentText)
    {
        if (string.IsNullOrWhiteSpace(documentText)) return false;
        var firstLine = documentText.Split('\n')[0].Trim();
        return string.Equals(firstLine, "math", StringComparison.OrdinalIgnoreCase);
    }
}
