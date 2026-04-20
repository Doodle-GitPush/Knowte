using NUnit.Framework;
using Antinote.Commands;

namespace Antinote.Tests.Commands;

[TestFixture]
public class SlashCommandRegistryTests
{
    private SlashCommandRegistry _registry = null!;

    [SetUp]
    public void SetUp() => _registry = new SlashCommandRegistry();

    [Test]
    public void GetAll_ReturnsSixCommands()
    {
        Assert.That(_registry.GetAll().Count, Is.EqualTo(6));
    }

    [Test]
    public void Filter_EmptyQuery_ReturnsAll()
    {
        Assert.That(_registry.Filter("").Count, Is.EqualTo(6));
    }

    [Test]
    public void Filter_PartialMatch_ReturnsMatches()
    {
        var results = _registry.Filter("da");
        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("date"));
    }

    [Test]
    public void Filter_NoMatch_ReturnsEmpty()
    {
        Assert.That(_registry.Filter("xyz").Count, Is.EqualTo(0));
    }

    [Test]
    public void Filter_CaseInsensitive()
    {
        var results = _registry.Filter("DA");
        Assert.That(results.Count, Is.EqualTo(1));
    }

    [Test]
    public void AllCommands_HaveNonEmptyInsertText()
    {
        foreach (var cmd in _registry.GetAll().Where(c => c.Name != "x"))
            Assert.That(cmd.InsertText, Is.Not.Empty, $"{cmd.Name} has empty InsertText");
    }
}
