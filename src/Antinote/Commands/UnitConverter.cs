using System.Text.RegularExpressions;

namespace Knowte.Commands;

/// <summary>
/// Parses and evaluates unit conversion expressions of the form "N unit1 to unit2".
/// Uses a lookup-table approach with fixed currency rates — no external dependencies.
/// </summary>
public static class UnitConverter
{
    // ── Input pattern ─────────────────────────────────────────────────────────

    private static readonly Regex InputPattern =
        new(@"^\s*(?<value>-?\d+(?:\.\d+)?)\s+(?<from>\S+)\s+to\s+(?<to>\S+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── Conversion graph: (fromKey, toKey) → factor or formula ───────────────
    // All non-temperature units are stored as a factor relative to a base unit.
    // Temperature uses special-cased formula conversion.

    // Base units: metre, gram, byte, sqmetre, USD
    // Pixel/rem handled separately.

    private static readonly Dictionary<string, double> ToBase = new(StringComparer.OrdinalIgnoreCase)
    {
        // Distance → metre
        ["m"]      = 1.0,
        ["metre"]  = 1.0,
        ["metres"] = 1.0,
        ["meter"]  = 1.0,
        ["meters"] = 1.0,
        ["km"]     = 1000.0,
        ["kilometre"] = 1000.0,
        ["kilometres"] = 1000.0,
        ["kilometer"] = 1000.0,
        ["kilometers"] = 1000.0,
        ["cm"]     = 0.01,
        ["centimetre"] = 0.01,
        ["centimeter"] = 0.01,
        ["mm"]     = 0.001,
        ["millimetre"] = 0.001,
        ["millimeter"] = 0.001,
        ["mi"]     = 1609.344,
        ["mile"]   = 1609.344,
        ["miles"]  = 1609.344,
        ["ft"]     = 0.3048,
        ["foot"]   = 0.3048,
        ["feet"]   = 0.3048,
        ["in"]     = 0.0254,
        ["inch"]   = 0.0254,
        ["inches"] = 0.0254,
        ["yd"]     = 0.9144,
        ["yard"]   = 0.9144,
        ["yards"]  = 0.9144,

        // Weight → gram
        ["g"]      = 1.0,
        ["gram"]   = 1.0,
        ["grams"]  = 1.0,
        ["kg"]     = 1000.0,
        ["kilogram"] = 1000.0,
        ["kilograms"] = 1000.0,
        ["lb"]     = 453.59237,
        ["lbs"]    = 453.59237,
        ["pound"]  = 453.59237,
        ["pounds"] = 453.59237,
        ["oz"]     = 28.349523125,
        ["ounce"]  = 28.349523125,
        ["ounces"] = 28.349523125,
        ["t"]      = 1_000_000.0,   // metric tonne
        ["tonne"]  = 1_000_000.0,
        ["tonnes"] = 1_000_000.0,

        // Digital storage → byte
        ["b"]   = 1.0,
        ["byte"] = 1.0,
        ["bytes"] = 1.0,
        ["kb"]  = 1024.0,
        ["kib"] = 1024.0,
        ["mb"]  = 1_048_576.0,
        ["mib"] = 1_048_576.0,
        ["gb"]  = 1_073_741_824.0,
        ["gib"] = 1_073_741_824.0,
        ["tb"]  = 1_099_511_627_776.0,
        ["tib"] = 1_099_511_627_776.0,

        // Area → square metre
        ["sqm"]   = 1.0,
        ["m2"]    = 1.0,
        ["m²"]    = 1.0,
        ["sqft"]  = 0.09290304,
        ["ft2"]   = 0.09290304,
        ["ft²"]   = 0.09290304,
        ["sqin"]  = 0.00064516,
        ["in2"]   = 0.00064516,
        ["sqkm"]  = 1_000_000.0,
        ["km2"]   = 1_000_000.0,
        ["sqmi"]  = 2_589_988.110336,
        ["acre"]  = 4046.8564224,
        ["acres"] = 4046.8564224,
        ["ha"]    = 10_000.0,
        ["hectare"]  = 10_000.0,
        ["hectares"] = 10_000.0,

        // Currency → USD (fixed rates, April 2025 approximations)
        ["usd"]     = 1.0,
        ["$"]       = 1.0,
        ["dollar"]  = 1.0,
        ["dollars"] = 1.0,
        ["eur"]     = 1.0869565,
        ["€"]       = 1.0869565,
        ["euro"]    = 1.0869565,
        ["euros"]   = 1.0869565,
        ["gbp"]     = 1.2658228,
        ["£"]       = 1.2658228,
        ["pound"]   = 1.2658228,
        ["pounds"]  = 1.2658228,
        ["jpy"]     = 0.006711,
        ["¥"]       = 0.006711,
        ["yen"]     = 0.006711,
        ["cad"]     = 0.7353,
        ["aud"]     = 0.6410,
        ["chf"]     = 1.1236,
        ["franc"]   = 1.1236,
        ["francs"]  = 1.1236,
        ["cny"]     = 0.13755,
        ["yuan"]    = 0.13755,
        ["rmb"]     = 0.13755,
        ["inr"]     = 0.011905,  // 1 INR ≈ 0.011905 USD  (1 USD ≈ 84 INR)
        ["₹"]       = 0.011905,
        ["rupee"]   = 0.011905,
        ["rupees"]  = 0.011905,
        ["krw"]     = 0.000724,  // 1 KRW ≈ 0.000724 USD
        ["won"]     = 0.000724,
        ["mxn"]     = 0.051,     // 1 MXN ≈ 0.051 USD
        ["peso"]    = 0.051,
        ["pesos"]   = 0.051,
        ["brl"]     = 0.178,     // 1 BRL ≈ 0.178 USD
        ["real"]    = 0.178,
        ["reais"]   = 0.178,
        ["sgd"]     = 0.743,     // 1 SGD ≈ 0.743 USD
        ["hkd"]     = 0.1282,    // 1 HKD ≈ 0.1282 USD
        ["nok"]     = 0.091,     // 1 NOK ≈ 0.091 USD
        ["sek"]     = 0.091,     // 1 SEK ≈ 0.091 USD
        ["dkk"]     = 0.1456,    // 1 DKK ≈ 0.1456 USD
        ["aed"]     = 0.2723,    // 1 AED ≈ 0.2723 USD (dirham, pegged)
        ["dirham"]  = 0.2723,
        ["dirhams"] = 0.2723,
        ["sar"]     = 0.2667,    // 1 SAR ≈ 0.2667 USD (riyal, pegged)
        ["riyal"]   = 0.2667,
        ["riyals"]  = 0.2667,
    };

    // Display labels when converting FROM base
    private static readonly Dictionary<string, string> DisplayLabel = new(StringComparer.OrdinalIgnoreCase)
    {
        // Distance
        ["m"]      = "m",   ["metre"] = "m",   ["metres"] = "m",
        ["meter"]  = "m",   ["meters"] = "m",
        ["km"]     = "km",  ["kilometre"] = "km", ["kilometres"] = "km",
        ["kilometer"] = "km", ["kilometers"] = "km",
        ["cm"]     = "cm",  ["centimetre"] = "cm",  ["centimeter"] = "cm",
        ["mm"]     = "mm",  ["millimetre"] = "mm",  ["millimeter"] = "mm",
        ["mi"]     = "mi",  ["mile"] = "mi",    ["miles"] = "mi",
        ["ft"]     = "ft",  ["foot"] = "ft",    ["feet"] = "ft",
        ["in"]     = "in",  ["inch"] = "in",    ["inches"] = "in",
        ["yd"]     = "yd",  ["yard"] = "yd",    ["yards"] = "yd",

        // Weight
        ["g"]      = "g",   ["gram"] = "g",     ["grams"] = "g",
        ["kg"]     = "kg",  ["kilogram"] = "kg", ["kilograms"] = "kg",
        ["lb"]     = "lbs", ["lbs"] = "lbs",    ["pound"] = "lbs", ["pounds"] = "lbs",
        ["oz"]     = "oz",  ["ounce"] = "oz",   ["ounces"] = "oz",
        ["t"]      = "t",   ["tonne"] = "t",    ["tonnes"] = "t",

        // Digital storage
        ["b"]    = "B",   ["byte"] = "B",   ["bytes"] = "B",
        ["kb"]   = "KB",  ["kib"]  = "KB",
        ["mb"]   = "MB",  ["mib"]  = "MB",
        ["gb"]   = "GB",  ["gib"]  = "GB",
        ["tb"]   = "TB",  ["tib"]  = "TB",

        // Area
        ["sqm"]  = "m²",  ["m2"]   = "m²",  ["m²"]   = "m²",
        ["sqft"] = "ft²", ["ft2"]  = "ft²", ["ft²"]  = "ft²",
        ["sqkm"] = "km²", ["km2"]  = "km²",
        ["sqmi"] = "mi²",
        ["acre"] = "acres", ["acres"] = "acres",
        ["ha"]   = "ha",  ["hectare"] = "ha", ["hectares"] = "ha",

        // Currency (display as uppercase code)
        ["usd"] = "USD", ["$"]  = "USD", ["dollar"]  = "USD", ["dollars"] = "USD",
        ["eur"] = "EUR", ["€"]  = "EUR", ["euro"]    = "EUR", ["euros"]   = "EUR",
        ["gbp"] = "GBP", ["£"]  = "GBP", ["pound"]   = "GBP", ["pounds"]  = "GBP",
        ["jpy"] = "JPY", ["¥"]  = "JPY", ["yen"]     = "JPY",
        ["cad"] = "CAD",
        ["aud"] = "AUD",
        ["chf"] = "CHF", ["franc"] = "CHF", ["francs"] = "CHF",
        ["cny"] = "CNY", ["yuan"] = "CNY", ["rmb"] = "CNY",
        ["inr"] = "INR", ["₹"]  = "INR", ["rupee"]   = "INR", ["rupees"]  = "INR",
        ["krw"] = "KRW", ["won"] = "KRW",
        ["mxn"] = "MXN", ["peso"] = "MXN", ["pesos"] = "MXN",
        ["brl"] = "BRL", ["real"] = "BRL", ["reais"] = "BRL",
        ["sgd"] = "SGD",
        ["hkd"] = "HKD",
        ["nok"] = "NOK",
        ["sek"] = "SEK",
        ["dkk"] = "DKK",
        ["aed"] = "AED", ["dirham"] = "AED", ["dirhams"] = "AED",
        ["sar"] = "SAR", ["riyal"]  = "SAR", ["riyals"]  = "SAR",
    };

    // ── Category grouping so we don't cross-convert kg→metres ────────────────

    private static readonly HashSet<string> DistanceUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "m","metre","metres","meter","meters",
        "km","kilometre","kilometres","kilometer","kilometers",
        "cm","centimetre","centimeter",
        "mm","millimetre","millimeter",
        "mi","mile","miles",
        "ft","foot","feet",
        "in","inch","inches",
        "yd","yard","yards",
    };

    private static readonly HashSet<string> WeightUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "g","gram","grams","kg","kilogram","kilograms",
        "lb","lbs","pound","pounds","oz","ounce","ounces",
        "t","tonne","tonnes",
    };

    private static readonly HashSet<string> StorageUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "b","byte","bytes","kb","kib","mb","mib","gb","gib","tb","tib",
    };

    private static readonly HashSet<string> AreaUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "sqm","m2","m²","sqft","ft2","ft²","sqin","in2","sqkm","km2","sqmi",
        "acre","acres","ha","hectare","hectares",
    };

    private static readonly HashSet<string> CurrencyUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "usd","$","dollar","dollars",
        "eur","€","euro","euros",
        "gbp","£","pound","pounds",
        "jpy","¥","yen",
        "cad","aud",
        "chf","franc","francs",
        "cny","yuan","rmb",
        "inr","₹","rupee","rupees",
        "krw","won",
        "mxn","peso","pesos",
        "brl","real","reais",
        "sgd","hkd","nok","sek","dkk",
        "aed","dirham","dirhams",
        "sar","riyal","riyals",
    };

    private static readonly HashSet<string> TemperatureUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "c","celsius","f","fahrenheit","k","kelvin",
    };

    private static readonly HashSet<string> PxRemUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "px","rem",
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses "N unit1 to unit2" and returns a formatted result string,
    /// or null if the expression is not parseable / units are unknown.
    /// </summary>
    public static string? TryConvert(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var m = InputPattern.Match(input);
        if (!m.Success) return null;

        var value  = double.Parse(m.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture);
        var from   = m.Groups["from"].Value;
        var to     = m.Groups["to"].Value;

        // Temperature — special formulas
        if (TemperatureUnits.Contains(from) && TemperatureUnits.Contains(to))
            return ConvertTemperature(value, from, to);

        // px ↔ rem (base 16)
        if (PxRemUnits.Contains(from) && PxRemUnits.Contains(to))
            return ConvertPxRem(value, from, to);

        // Factor-based conversions
        if (!ToBase.TryGetValue(from, out var fromFactor)) return null;
        if (!ToBase.TryGetValue(to,   out var toFactor))   return null;

        // Reject cross-category conversions
        if (!SameCategory(from, to)) return null;

        var result = value * fromFactor / toFactor;

        if (!DisplayLabel.TryGetValue(to, out var label))
            label = to.ToUpper();

        return FormatResult(result, label, CurrencyUnits.Contains(to));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool SameCategory(string a, string b)
    {
        if (DistanceUnits.Contains(a) && DistanceUnits.Contains(b)) return true;
        if (WeightUnits.Contains(a)   && WeightUnits.Contains(b))   return true;
        if (StorageUnits.Contains(a)  && StorageUnits.Contains(b))  return true;
        if (AreaUnits.Contains(a)     && AreaUnits.Contains(b))     return true;
        if (CurrencyUnits.Contains(a) && CurrencyUnits.Contains(b)) return true;
        return false;
    }

    private static string? ConvertTemperature(double value, string from, string to)
    {
        // Normalise to Celsius first
        double celsius = from.ToLower() switch
        {
            "c" or "celsius"    => value,
            "f" or "fahrenheit" => (value - 32) * 5.0 / 9.0,
            "k" or "kelvin"     => value - 273.15,
            _ => double.NaN
        };
        if (double.IsNaN(celsius)) return null;

        double result = to.ToLower() switch
        {
            "c" or "celsius"    => celsius,
            "f" or "fahrenheit" => celsius * 9.0 / 5.0 + 32,
            "k" or "kelvin"     => celsius + 273.15,
            _ => double.NaN
        };
        if (double.IsNaN(result)) return null;

        string label = to.ToLower() switch
        {
            "c" or "celsius"    => "°C",
            "f" or "fahrenheit" => "°F",
            "k" or "kelvin"     => "K",
            _                   => to.ToUpper(),
        };

        // Show whole number if result is integer, else 2 dp
        var formatted = result == Math.Truncate(result)
            ? ((long)result).ToString()
            : Math.Round(result, 2).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        return $"{formatted} {label}";
    }

    private static string? ConvertPxRem(double value, string from, string to)
    {
        const double baseSize = 16.0;

        double result = from.ToLower() switch
        {
            "px"  => to.ToLower() == "rem" ? value / baseSize : value,
            "rem" => to.ToLower() == "px"  ? value * baseSize : value,
            _     => double.NaN
        };
        if (double.IsNaN(result)) return null;

        var formatted = result == Math.Truncate(result)
            ? ((long)result).ToString()
            : Math.Round(result, 4).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

        return $"{formatted} {to.ToLower()}";
    }

    private static string FormatResult(double result, string label, bool isCurrency)
    {
        string formatted;
        if (isCurrency)
        {
            formatted = result.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }
        else if (result == Math.Truncate(result) && Math.Abs(result) < 1e12)
        {
            formatted = ((long)result).ToString();
        }
        else
        {
            // Up to 2 significant decimal places, trimming trailing zeros
            var rounded = Math.Round(result, 2);
            formatted = rounded.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }
        return $"{formatted} {label}";
    }
}
