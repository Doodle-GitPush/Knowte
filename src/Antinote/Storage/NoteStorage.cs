using System.IO;

namespace Knowte.Storage;

public class NoteStorage
{
    private readonly string _notesDir;

    public NoteStorage(string notesDir) { _notesDir = notesDir; }

    public NoteStorage() : this(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Knowte", "notes"))
    { }

    public string TodayName => DateTime.Today.ToString("yyyy-MM-dd");

    public string GetTodayFilePath() => Path.Combine(_notesDir, $"{DateTime.Today:yyyy-MM-dd}.md");

    public List<string> GetAllNoteNames()
    {
        if (!Directory.Exists(_notesDir)) return [];
        return Directory.GetFiles(_notesDir, "*.md")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .OrderBy(n => n)
            .ToList();
    }

    public string Load(string name)
    {
        var path = Path.Combine(_notesDir, $"{name}.md");
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    public void Save(string name, string content)
    {
        Directory.CreateDirectory(_notesDir);
        File.WriteAllText(Path.Combine(_notesDir, $"{name}.md"), content);
    }

    public string CreateNewNote()
    {
        Directory.CreateDirectory(_notesDir);
        int i = 1;
        while (true)
        {
            var name = $"note-{i}";
            var path = Path.Combine(_notesDir, $"{name}.md");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
                return name;
            }
            i++;
        }
    }

    // Backward-compat overloads
    public string LoadToday() => Load(TodayName);

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
