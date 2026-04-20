using NUnit.Framework;
using Antinote.Startup;

namespace Antinote.Tests.Startup;

[TestFixture]
public class StartupManagerTests
{
    [Test]
    public void RegistryKeyName_IsCorrect()
    {
        Assert.That(StartupManager.AppName, Is.EqualTo("Antinote"));
    }
}
