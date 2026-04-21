using NUnit.Framework;
using Antinote.Commands;

namespace Antinote.Tests.Commands;

[TestFixture]
public class DocumentModeDetectorTests
{
    // ── Backward-compat single-word (no semicolon) ───────────────────────────

    [Test] public void Detect_Math_ReturnsMath() =>
        Assert.That(DocumentModeDetector.Detect("math\n2+2"), Is.EqualTo(DocumentMode.Math));

    [Test] public void Detect_MathUpperCase_ReturnsMath() =>
        Assert.That(DocumentModeDetector.Detect("MATH\n2+2"), Is.EqualTo(DocumentMode.Math));

    [Test] public void Detect_List_ReturnsList() =>
        Assert.That(DocumentModeDetector.Detect("list\n- item"), Is.EqualTo(DocumentMode.List));

    [Test] public void Detect_Checklist_ReturnsChecklist() =>
        Assert.That(DocumentModeDetector.Detect("checklist\n- [ ] item"), Is.EqualTo(DocumentMode.Checklist));

    [Test] public void Detect_OtherFirstLine_ReturnsNone() =>
        Assert.That(DocumentModeDetector.Detect("hello\n2+2"), Is.EqualTo(DocumentMode.None));

    [Test] public void Detect_Empty_ReturnsNone() =>
        Assert.That(DocumentModeDetector.Detect(""), Is.EqualTo(DocumentMode.None));

    [Test] public void Detect_MathWithTrailingSpace_ReturnsMath() =>
        Assert.That(DocumentModeDetector.Detect("math  \n2+2"), Is.EqualTo(DocumentMode.Math));

    // ── Single mode with semicolon ────────────────────────────────────────────

    [Test] public void Detect_MathSemicolon_ReturnsMath() =>
        Assert.That(DocumentModeDetector.Detect("math;\n2+2"), Is.EqualTo(DocumentMode.Math));

    [Test] public void Detect_ChecklistSemicolon_ReturnsChecklist() =>
        Assert.That(DocumentModeDetector.Detect("checklist;\n- [ ] item"), Is.EqualTo(DocumentMode.Checklist));

    [Test] public void Detect_ConvertSemicolon_ReturnsConvert() =>
        Assert.That(DocumentModeDetector.Detect("convert;\n1 km to miles"), Is.EqualTo(DocumentMode.Convert));

    // ── Multi-mode combinations ───────────────────────────────────────────────

    [Test] public void Detect_MathAndChecklist_ReturnsBothFlags()
    {
        var mode = DocumentModeDetector.Detect("math; checklist;\nnote");
        Assert.That(mode.HasFlag(DocumentMode.Math), Is.True);
        Assert.That(mode.HasFlag(DocumentMode.Checklist), Is.True);
    }

    [Test] public void Detect_ConvertAndMath_ReturnsBothFlags()
    {
        var mode = DocumentModeDetector.Detect("convert; math;\nnote");
        Assert.That(mode.HasFlag(DocumentMode.Convert), Is.True);
        Assert.That(mode.HasFlag(DocumentMode.Math), Is.True);
    }

    [Test] public void Detect_AllModes_ReturnsAllFlags()
    {
        var mode = DocumentModeDetector.Detect("math; list; convert; paste;");
        Assert.That(mode.HasFlag(DocumentMode.Math), Is.True);
        Assert.That(mode.HasFlag(DocumentMode.List), Is.True);
        Assert.That(mode.HasFlag(DocumentMode.Convert), Is.True);
        Assert.That(mode.HasFlag(DocumentMode.Paste), Is.True);
    }

    [Test] public void Detect_TokenWithoutSemicolon_NotActivated()
    {
        // "math" without ';' alongside another token → not activated (multi-mode path)
        var mode = DocumentModeDetector.Detect("math list;");
        Assert.That(mode.HasFlag(DocumentMode.Math), Is.False);
        Assert.That(mode.HasFlag(DocumentMode.List), Is.True);
    }

    // ── IsModeName helper ─────────────────────────────────────────────────────

    [Test] public void IsModeName_Math_ReturnsTrue() =>
        Assert.That(DocumentModeDetector.IsModeName("math"), Is.True);

    [Test] public void IsModeName_Unknown_ReturnsFalse() =>
        Assert.That(DocumentModeDetector.IsModeName("hello"), Is.False);

    [Test] public void IsModeName_CaseInsensitive() =>
        Assert.That(DocumentModeDetector.IsModeName("CHECKLIST"), Is.True);
}
