using NUnit.Framework;
using Antinote.Commands;

namespace Antinote.Tests.Commands;

[TestFixture]
public class MathEvaluatorTests
{
    private MathEvaluator _evaluator = null!;

    [SetUp]
    public void SetUp() => _evaluator = new MathEvaluator();

    [Test]
    public void Evaluate_SimpleAddition_ReturnsResult()
    {
        Assert.That(_evaluator.Evaluate("2 + 2"), Is.EqualTo("4"));
    }

    [Test]
    public void Evaluate_Multiplication_ReturnsResult()
    {
        Assert.That(_evaluator.Evaluate("3 * 7"), Is.EqualTo("21"));
    }

    [Test]
    public void Evaluate_DecimalResult_ReturnsRounded()
    {
        Assert.That(_evaluator.Evaluate("10 / 3"), Is.EqualTo("3.33"));
    }

    [Test]
    public void Evaluate_InvalidExpression_ReturnsNull()
    {
        Assert.That(_evaluator.Evaluate("hello world"), Is.Null);
    }

    [Test]
    public void Evaluate_EmptyString_ReturnsNull()
    {
        Assert.That(_evaluator.Evaluate(""), Is.Null);
    }

    [Test]
    public void TryFormatMathLine_ValidMathLine_ReturnsFormattedLine()
    {
        var result = _evaluator.TryFormatMathLine("= 2 + 2");
        Assert.That(result, Is.EqualTo("= 2 + 2 \u2192 4"));
    }

    [Test]
    public void TryFormatMathLine_AlreadyEvaluated_ReturnsNull()
    {
        Assert.That(_evaluator.TryFormatMathLine("= 2 + 2 \u2192 4"), Is.Null);
    }

    [Test]
    public void TryFormatMathLine_NotMathLine_ReturnsNull()
    {
        Assert.That(_evaluator.TryFormatMathLine("just a note"), Is.Null);
    }
}
