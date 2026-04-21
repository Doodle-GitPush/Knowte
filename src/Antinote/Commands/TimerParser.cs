using System.Text.RegularExpressions;

namespace Knowte.Commands;

public static class TimerParser
{
    // Matches optional hours, minutes, seconds with flexible suffixes
    private static readonly Regex _pattern = new(
        @"(?:(\d+)\s*(?:h(?:rs?|ours?)?)[\s,]*)?(?:(\d+)\s*(?:m(?:ins?|inutes?)?)[\s,]*)?(?:(\d+)\s*(?:s(?:ec(?:onds?)?)?))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Also handle plain "90 min" style (only one group)
    private static readonly Regex _singleMin = new(
        @"^(\d+)\s*(?:m(?:ins?|inutes?)?)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _singleSec = new(
        @"^(\d+)\s*(?:s(?:ec(?:onds?)?)?)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _singleHour = new(
        @"^(\d+)\s*(?:h(?:rs?|ours?)?)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static TimeSpan? TryParse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim();

        var m = _pattern.Match(text);
        if (!m.Success) return null;

        int hours = 0, minutes = 0, seconds = 0;
        bool any = false;

        if (m.Groups[1].Success) { hours   = int.Parse(m.Groups[1].Value); any = true; }
        if (m.Groups[2].Success) { minutes = int.Parse(m.Groups[2].Value); any = true; }
        if (m.Groups[3].Success) { seconds = int.Parse(m.Groups[3].Value); any = true; }

        if (!any) return null;

        var ts = new TimeSpan(hours, minutes, seconds);
        return ts == TimeSpan.Zero ? null : ts;
    }
}
