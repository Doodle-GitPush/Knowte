using System.Text.RegularExpressions;

namespace Knowte.Commands;

public static class NaturalLanguageMath
{
    private static readonly Dictionary<string, string> WordNumbers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["one"] = "1", ["two"] = "2", ["three"] = "3", ["four"] = "4", ["five"] = "5",
        ["six"] = "6", ["seven"] = "7", ["eight"] = "8", ["nine"] = "9", ["ten"] = "10"
    };

    private static readonly (Regex Pattern, string Replacement)[] Rules =
    [
        (new Regex(@"\bdivided\s+(?:by|in)\b", RegexOptions.IgnoreCase), "/"),
        (new Regex(@"\bmultiplied\s+by\b", RegexOptions.IgnoreCase), "*"),
        (new Regex(@"\btimes\b", RegexOptions.IgnoreCase), "*"),
        (new Regex(@"\bplus\b", RegexOptions.IgnoreCase), "+"),
        (new Regex(@"\bminus\b", RegexOptions.IgnoreCase), "-"),
        (new Regex(@"\bpercent\s+of\b", RegexOptions.IgnoreCase), "/100 *"),
    ];

    // Matches "number word operator number word" patterns like "20 cookies divided in 6 people"
    private static readonly Regex NaturalSentence =
        new(@"(\d+(?:\.\d+)?)\s+\w+\s+(divided\s+(?:by|in)|multiplied\s+by|times|plus|minus|percent\s+of)\s+(\d+(?:\.\d+)?)\s+\w+",
            RegexOptions.IgnoreCase);

    private static readonly Regex PowerOp =
        new(@"(\d+(?:\.\d+)?)\s*\^\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);

    private static readonly Regex SqrtFix =
        new(@"\bsqrt\s*\(", RegexOptions.IgnoreCase);

    private static readonly Regex BarePercent =
        new(@"(\d+(?:\.\d+)?)%");

    public static string Normalize(string input)
    {
        // First, try to extract natural sentence pattern like "20 cookies divided in 6 people"
        var sentenceMatch = NaturalSentence.Match(input);
        if (sentenceMatch.Success)
        {
            input = $"{sentenceMatch.Groups[1].Value} {sentenceMatch.Groups[2].Value} {sentenceMatch.Groups[3].Value}";
        }

        // Pow operator
        input = PowerOp.Replace(input, m => $"Pow({m.Groups[1].Value},{m.Groups[2].Value})");

        // sqrt case fix
        input = SqrtFix.Replace(input, "Sqrt(");

        // bare percentage
        input = BarePercent.Replace(input, "($1/100)");

        // Replace word numbers
        foreach (var (word, digit) in WordNumbers)
            input = Regex.Replace(input, $@"\b{word}\b", digit, RegexOptions.IgnoreCase);

        // Apply operator rules
        foreach (var (pattern, replacement) in Rules)
            input = pattern.Replace(input, replacement);

        return input;
    }
}
