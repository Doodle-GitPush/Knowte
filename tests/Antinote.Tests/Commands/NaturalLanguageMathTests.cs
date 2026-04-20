using NUnit.Framework;
using Antinote.Commands;

namespace Antinote.Tests.Commands;

[TestFixture]
public class NaturalLanguageMathTests
{
    [Test]
    public void Normalize_DividedBy_ReplacesWithSlash()
    {
        Assert.That(NaturalLanguageMath.Normalize("20 divided by 4"), Is.EqualTo("20 / 4"));
    }

    [Test]
    public void Normalize_DividedIn_ReplacesWithSlash()
    {
        Assert.That(NaturalLanguageMath.Normalize("20 divided in 4"), Is.EqualTo("20 / 4"));
    }

    [Test]
    public void Normalize_Times_ReplacesWithStar()
    {
        Assert.That(NaturalLanguageMath.Normalize("3 times 7"), Is.EqualTo("3 * 7"));
    }

    [Test]
    public void Normalize_MultipliedBy_ReplacesWithStar()
    {
        Assert.That(NaturalLanguageMath.Normalize("3 multiplied by 7"), Is.EqualTo("3 * 7"));
    }

    [Test]
    public void Normalize_Plus_ReplacesWithPlus()
    {
        Assert.That(NaturalLanguageMath.Normalize("5 plus 3"), Is.EqualTo("5 + 3"));
    }

    [Test]
    public void Normalize_Minus_ReplacesWithMinus()
    {
        Assert.That(NaturalLanguageMath.Normalize("10 minus 4"), Is.EqualTo("10 - 4"));
    }

    [Test]
    public void Normalize_PercentOf_ReplacesCorrectly()
    {
        Assert.That(NaturalLanguageMath.Normalize("20 percent of 80"), Is.EqualTo("20 /100 * 80"));
    }

    [Test]
    public void Normalize_WordNumbers_ReplacedWithDigits()
    {
        Assert.That(NaturalLanguageMath.Normalize("six plus two"), Is.EqualTo("6 + 2"));
    }

    [Test]
    public void Normalize_IgnoresUnrelatedText()
    {
        var result = NaturalLanguageMath.Normalize("hello world");
        Assert.That(result, Is.EqualTo("hello world"));
    }

    [Test]
    public void Normalize_NaturalSentence_ExtractsExpression()
    {
        // "20 cookies divided in 6 people" → "20 / 6"
        // extra words like "cookies" and "people" are stripped
        var result = NaturalLanguageMath.Normalize("20 cookies divided in 6 people");
        Assert.That(result, Is.EqualTo("20 / 6"));
    }

    [Test]
    public void Normalize_Caret_ConvertsToPow()
    {
        Assert.That(NaturalLanguageMath.Normalize("2^3"), Is.EqualTo("Pow(2,3)"));
    }

    [Test]
    public void Normalize_CaretWithSpaces_ConvertsToPow()
    {
        Assert.That(NaturalLanguageMath.Normalize("2 ^ 3"), Is.EqualTo("Pow(2,3)"));
    }

    [Test]
    public void Normalize_SqrtLowercase_FixesCase()
    {
        Assert.That(NaturalLanguageMath.Normalize("sqrt(9)"), Is.EqualTo("Sqrt(9)"));
    }

    [Test]
    public void Normalize_BarePercent_ConvertsToFraction()
    {
        Assert.That(NaturalLanguageMath.Normalize("50%"), Is.EqualTo("(50/100)"));
    }
}
