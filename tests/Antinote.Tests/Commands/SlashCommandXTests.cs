using NUnit.Framework;
using Knowte.Commands;

namespace Knowte.Tests.Commands;

[TestFixture]
public class SlashCommandXTests
{
    [Test]
    public void ToggleChecklistLine_Unchecked_BecomesChecked()
    {
        var result = SlashCommandEngine.ToggleChecklistOnLine("- [ ] Buy milk");
        Assert.That(result, Is.EqualTo("- [x] Buy milk"));
    }

    [Test]
    public void ToggleChecklistLine_Checked_BecomesUnchecked()
    {
        var result = SlashCommandEngine.ToggleChecklistOnLine("- [x] Buy milk");
        Assert.That(result, Is.EqualTo("- [ ] Buy milk"));
    }

    [Test]
    public void ToggleChecklistLine_NoCheckbox_ReturnsNull()
    {
        var result = SlashCommandEngine.ToggleChecklistOnLine("just a note");
        Assert.That(result, Is.Null);
    }
}
