using NUnit.Framework;
using Knowte.Commands;

namespace Knowte.Tests.Commands;

[TestFixture]
public class PasteModeDetectorTests
{
    [Test] public void Detect_Paste_ReturnsPaste() =>
        Assert.That(DocumentModeDetector.Detect("paste\nsome text"), Is.EqualTo(DocumentMode.Paste));

    [Test] public void Detect_PasteUpperCase_ReturnsPaste() =>
        Assert.That(DocumentModeDetector.Detect("PASTE"), Is.EqualTo(DocumentMode.Paste));

    [Test] public void Detect_PasteWithTrailingSpace_ReturnsPaste() =>
        Assert.That(DocumentModeDetector.Detect("paste  \ntext"), Is.EqualTo(DocumentMode.Paste));

    [Test] public void Detect_PasteOtherFirstLine_ReturnsNone() =>
        Assert.That(DocumentModeDetector.Detect("hello\nsome text"), Is.EqualTo(DocumentMode.None));

    [Test] public void Detect_PasteEmpty_ReturnsNone() =>
        Assert.That(DocumentModeDetector.Detect(""), Is.EqualTo(DocumentMode.None));
}
