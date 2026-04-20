using NUnit.Framework;
using Antinote.Commands;

namespace Antinote.Tests.Commands;

[TestFixture]
public class DocumentModeDetectorTests
{
    [Test] public void Detect_Math_Returnsmath() =>
        Assert.That(DocumentModeDetector.Detect("math\n2+2"), Is.EqualTo(DocumentMode.Math));

    [Test] public void Detect_MathUpperCase_ReturnsMath() =>
        Assert.That(DocumentModeDetector.Detect("MATH\n2+2"), Is.EqualTo(DocumentMode.Math));

    [Test] public void Detect_List_ReturnsList() =>
        Assert.That(DocumentModeDetector.Detect("list\n- item"), Is.EqualTo(DocumentMode.List));

    [Test] public void Detect_ListUpperCase_ReturnsList() =>
        Assert.That(DocumentModeDetector.Detect("LIST"), Is.EqualTo(DocumentMode.List));

    [Test] public void Detect_Checklist_ReturnsChecklist() =>
        Assert.That(DocumentModeDetector.Detect("checklist\n- [ ] item"), Is.EqualTo(DocumentMode.Checklist));

    [Test] public void Detect_ChecklistUpperCase_ReturnsChecklist() =>
        Assert.That(DocumentModeDetector.Detect("CHECKLIST"), Is.EqualTo(DocumentMode.Checklist));

    [Test] public void Detect_OtherFirstLine_ReturnsNone() =>
        Assert.That(DocumentModeDetector.Detect("hello\n2+2"), Is.EqualTo(DocumentMode.None));

    [Test] public void Detect_Empty_ReturnsNone() =>
        Assert.That(DocumentModeDetector.Detect(""), Is.EqualTo(DocumentMode.None));

    [Test] public void Detect_MathWithTrailingSpace_ReturnsMath() =>
        Assert.That(DocumentModeDetector.Detect("math  \n2+2"), Is.EqualTo(DocumentMode.Math));
}
