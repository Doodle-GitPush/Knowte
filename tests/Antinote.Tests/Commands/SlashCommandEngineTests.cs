using NUnit.Framework;
using Antinote.Commands;

namespace Antinote.Tests.Commands;

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
}
