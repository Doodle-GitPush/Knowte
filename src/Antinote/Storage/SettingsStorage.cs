using System.IO;
using System.Text.Json;
using Knowte.Hotkey;

namespace Knowte.Storage;

public class KnowteSettings
{
    public int HotkeyModifiers { get; set; } = HotkeyManager.MOD_ALT | HotkeyManager.MOD_CONTROL;
    public int HotkeyVk { get; set; } = 0x4E; // N
    public bool IsDarkMode { get; set; } = false;
}

public static class SettingsStorage
{
    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Knowte", "settings.json");

    public static KnowteSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<KnowteSettings>(json) ?? new KnowteSettings();
            }
        }
        catch { }
        return new KnowteSettings();
    }

    public static void Save(KnowteSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath,
                JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
