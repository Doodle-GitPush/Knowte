using System.Text.RegularExpressions;

namespace Antinote.Commands;

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

    public static string Normalize(string input)
    {
        // First, try to extract natural sentence pattern like "20 cookies divided in 6 people"
        var sentenceMatch = NaturalSentence.Match(input);
        if (sentenceMatch.Success)
        {
            input = $"{sentenceMatch.Groups[1].Value} {sentenceMatch.Groups[2].Value} {sentenceMatch.Groups[3].Value}";
        }

        // Replace word numbers
        foreach (var (word, digit) in WordNumbers)
            input = Regex.Replace(input, $@"\b{word}\b", digit, RegexOptions.IgnoreCase);

        // Apply operator rules
        foreach (var (pattern, replacement) in Rules)
            input = pattern.Replace(input, replacement);

        return input;
    }
}
