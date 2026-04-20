using System.IO;

namespace Antinote.Storage;

public class NoteStorage
{
    private readonly string _notesDir;

    public NoteStorage(string notesDir)
    {
        _notesDir = notesDir;
    }

    public NoteStorage() : this(
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Antinote", "notes"))
    { }

    public string GetTodayFilePath() =>
        Path.Combine(_notesDir, $"{DateTime.Today:yyyy-MM-dd}.md");

    public string LoadToday()
    {
        var path = GetTodayFilePath();
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    public void Save(string content)
    {
        Directory.CreateDirectory(_notesDir);
        File.WriteAllText(GetTodayFilePath(), content);
    }

    public void OpenNotesFolder()
    {
        Directory.CreateDirectory(_notesDir);
        System.Diagnostics.Process.Start("explorer.exe", _notesDir);
    }
}
