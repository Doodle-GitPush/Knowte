using System.IO;
using NUnit.Framework;
using Knowte.Storage;

namespace Knowte.Tests.Storage;

[TestFixture]
public class NoteStorageTests
{
    private string _tempDir = null!;
    private NoteStorage _storage = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _storage = new NoteStorage(_tempDir);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_tempDir, recursive: true);

    [Test]
    public void GetTodayFilePath_ReturnsCorrectFormat()
    {
        var expected = Path.Combine(_tempDir, $"{DateTime.Today:yyyy-MM-dd}.md");
        Assert.That(_storage.GetTodayFilePath(), Is.EqualTo(expected));
    }

    [Test]
    public void LoadToday_ReturnsEmpty_WhenFileDoesNotExist()
    {
        Assert.That(_storage.LoadToday(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void Save_And_LoadToday_RoundTrips()
    {
        _storage.Save("# Hello\nThis is a note.");
        Assert.That(_storage.LoadToday(), Is.EqualTo("# Hello\nThis is a note."));
    }

    [Test]
    public void Save_CreatesDirectoryIfMissing()
    {
        var nested = Path.Combine(_tempDir, "sub", "notes");
        var storage = new NoteStorage(nested);
        storage.Save("test");
        Assert.That(File.Exists(storage.GetTodayFilePath()), Is.True);
    }
}
