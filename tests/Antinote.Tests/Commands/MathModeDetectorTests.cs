using NUnit.Framework;
using Antinote.Commands;

namespace Antinote.Tests.Commands;

[TestFixture]
public class MathModeDetectorTests
{
    [Test]
    public void IsActive_FirstLineMath_ReturnsTrue()
    {
        Assert.That(MathModeDetector.IsActive("math\n2 + 2"), Is.True);
    }

    [Test]
    public void IsActive_FirstLineMathUpperCase_ReturnsTrue()
    {
        Assert.That(MathModeDetector.IsActive("MATH\n2 + 2"), Is.True);
    }

    [Test]
    public void IsActive_FirstLineNotMath_ReturnsFalse()
    {
        Assert.That(MathModeDetector.IsActive("hello\n2 + 2"), Is.False);
    }

    [Test]
    public void IsActive_EmptyDocument_ReturnsFalse()
    {
        Assert.That(MathModeDetector.IsActive(""), Is.False);
    }

    [Test]
    public void IsActive_OnlyMath_ReturnsTrue()
    {
        Assert.That(MathModeDetector.IsActive("math"), Is.True);
    }

    [Test]
    public void IsActive_MathWithTrailingSpace_ReturnsTrue()
    {
        Assert.That(MathModeDetector.IsActive("math  \n2+2"), Is.True);
    }
}
