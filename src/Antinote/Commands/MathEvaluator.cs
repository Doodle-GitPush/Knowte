using NCalc;

namespace Antinote.Commands;

public class MathEvaluator
{
    public string? Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;
        try
        {
            var expr = new Expression(expression);
            var result = expr.Evaluate();
            if (result is double d)
                return Math.Round(d, 2).ToString();
            return result?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public string? TryFormatMathLine(string line)
    {
        if (!line.StartsWith("= ")) return null;
        if (line.Contains(" \u2192 ")) return null;

        var expression = line[2..].Trim();
        var result = Evaluate(expression);
        if (result == null) return null;

        return $"{line} \u2192 {result}";
    }
}
