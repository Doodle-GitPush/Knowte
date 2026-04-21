using NUnit.Framework;
using Knowte.Commands;

namespace Knowte.Tests.Commands;

[TestFixture]
public class SlashCommandEngineTests
{
    [Test]
    public void ExtractSlashQuery_SlashAtStart_ReturnsEmptyQuery()
    {
        var result = SlashCommandEngine.ExtractSlashQuery("/");
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void ExtractSlashQuery_SlashWithText_ReturnsQuery()
    {
        var result = SlashCommandEngine.ExtractSlashQuery("/ma");
        Assert.That(result, Is.EqualTo("ma"));
    }

    [Test]
    public void ExtractSlashQuery_NoSlash_ReturnsNull()
    {
        var result = SlashCommandEngine.ExtractSlashQuery("hello");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ExtractSlashQuery_SlashInMiddle_ReturnsNull()
    {
        var result = SlashCommandEngine.ExtractSlashQuery("hello /world");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ExtractSlashQuery_SlashWithSpace_ReturnsNull()
    {
        var result = SlashCommandEngine.ExtractSlashQuery("/ma th");
        Assert.That(result, Is.Null);
    }

    // List mode continuation
    [Test]
    public void GetEnterContinuation_ListMode_ListLine_ReturnsContinuation()
    {
        var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.List, "- Buy milk");
        Assert.That(result, Is.EqualTo("\n- "));
    }

    [Test]
    public void GetEnterContinuation_ListMode_EmptyItem_ReturnsNull()
    {
        var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.List, "- ");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetEnterContinuation_ListMode_FreeLine_ReturnsContinuation()
    {
        var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.List, "just a note");
        Assert.That(result, Is.EqualTo("\n- "));
    }

    // Checklist mode continuation
    [Test]
    public void GetEnterContinuation_ChecklistMode_UncheckedLine_ReturnsContinuation()
    {
        var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.Checklist, "- [ ] Buy milk");
        Assert.That(result, Is.EqualTo("\n- [ ] "));
    }

    [Test]
    public void GetEnterContinuation_ChecklistMode_CheckedLine_ReturnsContinuation()
    {
        var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.Checklist, "- [x] Done item");
        Assert.That(result, Is.EqualTo("\n- [ ] "));
    }

    [Test]
    public void GetEnterContinuation_ChecklistMode_EmptyItem_ReturnsNull()
    {
        var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.Checklist, "- [ ] ");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetEnterContinuation_ChecklistMode_FreeLine_ReturnsContinuation()
    {
        var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.Checklist, "just a note");
        Assert.That(result, Is.EqualTo("\n- [ ] "));
    }

    [Test]
    public void GetEnterContinuation_NoneMode_AnyLine_ReturnsPlain()
    {
        var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.None, "- some text");
        Assert.That(result, Is.EqualTo("\n"));
    }

    // Multi-mode combinations
    [Test]
    public void GetEnterContinuation_MathAndChecklist_ChecklistWins()
    {
        var mode = DocumentMode.Math | DocumentMode.Checklist;
        var result = SlashCommandEngine.GetEnterContinuation(mode, "- [ ] Buy milk");
        Assert.That(result, Is.EqualTo("\n- [ ] "));
    }

    [Test]
    public void GetEnterContinuation_MathAndList_ListWins()
    {
        var mode = DocumentMode.Math | DocumentMode.List;
        var result = SlashCommandEngine.GetEnterContinuation(mode, "- Buy milk");
        Assert.That(result, Is.EqualTo("\n- "));
    }

    [Test]
    public void GetEnterContinuation_ChecklistTakesPriorityOverList()
    {
        var mode = DocumentMode.List | DocumentMode.Checklist;
        var result = SlashCommandEngine.GetEnterContinuation(mode, "any line");
        Assert.That(result, Is.EqualTo("\n- [ ] "));
    }
}
