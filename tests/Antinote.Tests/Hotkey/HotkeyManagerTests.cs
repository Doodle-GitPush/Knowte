using NUnit.Framework;
using Knowte.Hotkey;

namespace Knowte.Tests.Hotkey;

[TestFixture]
public class HotkeyManagerTests
{
    [Test]
    public void ModifierFlags_CtrlAlt_CorrectValue()
    {
        // MOD_ALT = 0x0001, MOD_CONTROL = 0x0002
        Assert.That(HotkeyManager.MOD_ALT | HotkeyManager.MOD_CONTROL, Is.EqualTo(0x0003));
    }
}
