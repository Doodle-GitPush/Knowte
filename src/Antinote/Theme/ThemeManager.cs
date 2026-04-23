using System.Windows;
using System.Windows.Media;

namespace Knowte.Theme;

public static class ThemeManager
{
    public static bool IsDark { get; private set; }

    public static event Action? ThemeChanged;

    public static void Apply(bool dark)
    {
        IsDark = dark;
        var r = Application.Current.Resources;

        if (dark)
        {
            r["ThemeBg"]               = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            r["ThemeFg"]               = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            r["ThemePlaceholder"]      = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A));
            r["ThemeSubtle"]           = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A));
            r["ThemeMuted"]            = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
            r["ThemeBorder"]           = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
            r["ThemeControlBorder"]    = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A));
            r["ThemeBarButtonFg"]      = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
            r["ThemeBarButtonHoverFg"] = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            r["ThemeItemSelected"]     = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A));
            r["ThemeItemHover"]        = new SolidColorBrush(Color.FromRgb(0x28, 0x28, 0x28));
            r["ThemeInputBg"]          = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A));
            r["ThemePopupFg"]          = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
        }
        else
        {
            r["ThemeBg"]               = new SolidColorBrush(Colors.White);
            r["ThemeFg"]               = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
            r["ThemePlaceholder"]      = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
            r["ThemeSubtle"]           = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
            r["ThemeMuted"]            = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
            r["ThemeBorder"]           = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
            r["ThemeControlBorder"]    = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE));
            r["ThemeBarButtonFg"]      = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
            r["ThemeBarButtonHoverFg"] = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
            r["ThemeItemSelected"]     = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
            r["ThemeItemHover"]        = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
            r["ThemeInputBg"]          = new SolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xF8));
            r["ThemePopupFg"]          = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
        }

        ThemeChanged?.Invoke();
    }
}
