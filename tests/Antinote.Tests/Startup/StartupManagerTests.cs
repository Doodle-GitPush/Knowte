using NUnit.Framework;
using Knowte.Startup;

namespace Knowte.Tests.Startup;

[TestFixture]
public class StartupManagerTests
{
    [Test]
    public void RegistryKeyName_IsCorrect()
    {
        Assert.That(StartupManager.AppName, Is.EqualTo("Knowte"));
    }
}
