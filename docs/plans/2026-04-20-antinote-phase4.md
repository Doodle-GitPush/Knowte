# Antinote Phase 4 — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Generalise math mode into a first-line `DocumentMode` system (Math/List/Checklist), add Enter-key list continuation, fix popup DPI positioning, tighten UI, and improve the math pre-processor.

**Architecture:** Replace `MathModeDetector` with a `DocumentModeDetector` returning a `DocumentMode` enum. `HandleEnterKey` takes that mode and decides whether to continue a list prefix, exit an empty item, or format a math line. `NaturalLanguageMath.Normalize` gains `^`/Pow, sqrt casing, and bare-% fixes. Popup positioning is fixed by dividing screen coords by the DPI scale. XAML gets sizing/colour tweaks.

**Tech Stack:** .NET 10, WPF, AvalonEdit 6.3, NCalcSync 5.12, NUnit, NSubstitute

**Design doc:** `docs/plans/2026-04-20-antinote-phase4-design.md`

---

### Task 1: DocumentModeDetector

**Files:**
- Create: `src/Antinote/Commands/DocumentModeDetector.cs`
- Delete: `src/Antinote/Commands/MathModeDetector.cs`
- Create: `tests/Antinote.Tests/Commands/DocumentModeDetectorTests.cs`

**Step 1: Write failing tests**

Create `tests/Antinote.Tests/Commands/DocumentModeDetectorTests.cs`:
```csharp
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
```

**Step 2: Run to verify fail**
```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~DocumentModeDetectorTests"
```
Expected: FAIL — `DocumentModeDetector` not found.

**Step 3: Implement**

Create `src/Antinote/Commands/DocumentModeDetector.cs`:
```csharp
namespace Antinote.Commands;

public enum DocumentMode { None, Math, List, Checklist }

public static class DocumentModeDetector
{
    public static DocumentMode Detect(string documentText)
    {
        if (string.IsNullOrWhiteSpace(documentText)) return DocumentMode.None;
        var firstLine = documentText.Split('\n')[0].Trim();
        if (string.Equals(firstLine, "math",      StringComparison.OrdinalIgnoreCase)) return DocumentMode.Math;
        if (string.Equals(firstLine, "list",      StringComparison.OrdinalIgnoreCase)) return DocumentMode.List;
        if (string.Equals(firstLine, "checklist", StringComparison.OrdinalIgnoreCase)) return DocumentMode.Checklist;
        return DocumentMode.None;
    }
}
```

**Step 4: Run tests to verify pass**
```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~DocumentModeDetectorTests"
```
Expected: 9 passed.

**Step 5: Delete MathModeDetector and fix the one caller**

Delete `src/Antinote/Commands/MathModeDetector.cs`.

In `src/Antinote/Views/NoteWindow.xaml.cs`, replace:
```csharp
if (!MathModeDetector.IsActive(Editor.Text))
```
with:
```csharp
if (DocumentModeDetector.Detect(Editor.Text) != DocumentMode.Math)
```

Also delete `tests/Antinote.Tests/Commands/MathModeDetectorTests.cs` (replaced by DocumentModeDetectorTests).

**Step 6: Build to verify**
```bash
dotnet build src/Antinote
```
Expected: `Build succeeded.`

**Step 7: Commit**
```bash
git add src/Antinote/Commands/DocumentModeDetector.cs \
        tests/Antinote.Tests/Commands/DocumentModeDetectorTests.cs \
        src/Antinote/Views/NoteWindow.xaml.cs
git rm src/Antinote/Commands/MathModeDetector.cs \
       tests/Antinote.Tests/Commands/MathModeDetectorTests.cs
git commit -m "feat: replace MathModeDetector with general DocumentModeDetector"
```

---

### Task 2: Slim Down SlashCommandRegistry

**Files:**
- Modify: `src/Antinote/Commands/SlashCommandRegistry.cs`
- Modify: `tests/Antinote.Tests/Commands/SlashCommandRegistryTests.cs`

**Step 1: Remove mode commands from registry**

In `src/Antinote/Commands/SlashCommandRegistry.cs`, replace the full `_commands` list with:
```csharp
private readonly List<SlashCommand> _commands =
[
    new("x",       "Mark task done",      ""),
    new("date",    "Insert today's date", DateTime.Today.ToString("MMMM d, yyyy")),
    new("time",    "Insert current time", DateTime.Now.ToString("HH:mm")),
    new("divider", "Section separator",   "---"),
    new("code",    "Fenced code block",   "```\n\n```", 4),
    new("heading", "Section heading",     "## "),
];
```

**Step 2: Update registry tests**

In `tests/Antinote.Tests/Commands/SlashCommandRegistryTests.cs`, make these changes:

Replace `GetAll_ReturnsTenCommands`:
```csharp
[Test]
public void GetAll_ReturnsSixCommands()
{
    Assert.That(_registry.GetAll().Count, Is.EqualTo(6));
}
```

Replace `Filter_EmptyQuery_ReturnsAll`:
```csharp
[Test]
public void Filter_EmptyQuery_ReturnsAll()
{
    Assert.That(_registry.Filter("").Count, Is.EqualTo(6));
}
```

Replace `Filter_PartialMatch_ReturnsMatches` (math is gone; use "date"):
```csharp
[Test]
public void Filter_PartialMatch_ReturnsMatches()
{
    var results = _registry.Filter("da");
    Assert.That(results.Count, Is.EqualTo(1));
    Assert.That(results[0].Name, Is.EqualTo("date"));
}
```

Replace `Filter_CaseInsensitive`:
```csharp
[Test]
public void Filter_CaseInsensitive()
{
    var results = _registry.Filter("DA");
    Assert.That(results.Count, Is.EqualTo(1));
}
```

**Step 3: Run tests to verify pass**
```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~SlashCommandRegistryTests"
```
Expected: 6 passed.

**Step 4: Commit**
```bash
git add src/Antinote/Commands/SlashCommandRegistry.cs \
        tests/Antinote.Tests/Commands/SlashCommandRegistryTests.cs
git commit -m "feat: remove mode commands from slash registry (math/list/checklist/todo are now first-line modes)"
```

---

### Task 3: List/Checklist Enter Continuation

**Files:**
- Modify: `src/Antinote/Commands/SlashCommandEngine.cs`
- Modify: `tests/Antinote.Tests/Commands/SlashCommandEngineTests.cs`
- Modify: `src/Antinote/Views/NoteWindow.xaml.cs`

**Step 1: Write failing tests for GetEnterContinuation**

Add to `tests/Antinote.Tests/Commands/SlashCommandEngineTests.cs`:
```csharp
// List mode continuation
[Test]
public void GetEnterContinuation_ListMode_ListLine_ReturnsContinuation()
{
    var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.List, "- Buy milk");
    Assert.That(result, Is.EqualTo("\n- "));
}

[Test]
public void GetEnterContinuation_ListMode_EmptyItem_ReturnsNull()
{
    var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.List, "- ");
    Assert.That(result, Is.Null);
}

[Test]
public void GetEnterContinuation_ListMode_FreeLine_ReturnsPlain()
{
    var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.List, "just a note");
    Assert.That(result, Is.EqualTo("\n"));
}

// Checklist mode continuation
[Test]
public void GetEnterContinuation_ChecklistMode_UncheckedLine_ReturnsContinuation()
{
    var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.Checklist, "- [ ] Buy milk");
    Assert.That(result, Is.EqualTo("\n- [ ] "));
}

[Test]
public void GetEnterContinuation_ChecklistMode_CheckedLine_ReturnsContinuation()
{
    var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.Checklist, "- [x] Done item");
    Assert.That(result, Is.EqualTo("\n- [ ] "));
}

[Test]
public void GetEnterContinuation_ChecklistMode_EmptyItem_ReturnsNull()
{
    var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.Checklist, "- [ ] ");
    Assert.That(result, Is.Null);
}

[Test]
public void GetEnterContinuation_ChecklistMode_FreeLine_ReturnsPlain()
{
    var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.Checklist, "just a note");
    Assert.That(result, Is.EqualTo("\n"));
}

[Test]
public void GetEnterContinuation_NoneMode_AnyLine_ReturnsPlain()
{
    var result = SlashCommandEngine.GetEnterContinuation(DocumentMode.None, "- some text");
    Assert.That(result, Is.EqualTo("\n"));
}
```

**Step 2: Run to verify fail**
```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~SlashCommandEngineTests"
```
Expected: FAIL — `GetEnterContinuation` not found.

**Step 3: Add GetEnterContinuation + update HandleEnterKey**

In `src/Antinote/Commands/SlashCommandEngine.cs`:

Add this public static method before `ApplyCommand`:
```csharp
/// Returns the text to insert after Enter. Null means: remove the current line's list prefix, then insert plain newline.
public static string? GetEnterContinuation(DocumentMode mode, string lineText)
{
    return mode switch
    {
        DocumentMode.List when lineText == "- "    => null,
        DocumentMode.List when lineText.StartsWith("- ") => "\n- ",
        DocumentMode.Checklist when lineText == "- [ ] " => null,
        DocumentMode.Checklist when lineText.StartsWith("- [ ] ") || lineText.StartsWith("- [x] ") => "\n- [ ] ",
        _ => "\n"
    };
}
```

Replace the existing `HandleEnterKey()` with:
```csharp
public void HandleEnterKey(DocumentMode mode)
{
    var line = _editor.Document.GetLineByOffset(_editor.CaretOffset);
    var lineText = _editor.Document.GetText(line.Offset, line.Length);

    if (mode == DocumentMode.Math)
    {
        var formatted = _mathEvaluator.TryFormatMathLine(lineText);
        if (formatted != null)
        {
            _editor.Document.Replace(line.Offset, line.Length, formatted);
            _editor.CaretOffset = line.Offset + formatted.Length;
        }
        _editor.TextArea.PerformTextInput("\n");
        return;
    }

    var continuation = GetEnterContinuation(mode, lineText);
    if (continuation == null)
    {
        _editor.Document.Replace(line.Offset, line.Length, "");
        _editor.CaretOffset = line.Offset;
        _editor.TextArea.PerformTextInput("\n");
    }
    else
    {
        _editor.TextArea.PerformTextInput(continuation);
    }
}
```

**Step 4: Run tests to verify pass**
```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~SlashCommandEngineTests"
```
Expected: all passed.

**Step 5: Wire Shift+Enter and mode into NoteWindow**

In `src/Antinote/Views/NoteWindow.xaml.cs`, replace the Enter key block:
```csharp
if (e.Key == Key.Enter && !_slashPopup.IsOpen)
{
    _slashEngine.HandleEnterKey();
    e.Handled = true;
}
```
with:
```csharp
if (e.Key == Key.Enter && !_slashPopup.IsOpen)
{
    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        Editor.TextArea.PerformTextInput("\n");
    else
        _slashEngine.HandleEnterKey(DocumentModeDetector.Detect(Editor.Text));
    e.Handled = true;
}
```

**Step 6: Build to verify**
```bash
dotnet build src/Antinote
```
Expected: `Build succeeded.`

**Step 7: Commit**
```bash
git add src/Antinote/Commands/SlashCommandEngine.cs \
        tests/Antinote.Tests/Commands/SlashCommandEngineTests.cs \
        src/Antinote/Views/NoteWindow.xaml.cs
git commit -m "feat: add list/checklist Enter continuation, Shift+Enter plain newline"
```

---

### Task 4: NaturalLanguageMath Improvements

**Files:**
- Modify: `src/Antinote/Commands/NaturalLanguageMath.cs`
- Modify: `tests/Antinote.Tests/Commands/NaturalLanguageMathTests.cs`

**Step 1: Write failing tests**

Add to `tests/Antinote.Tests/Commands/NaturalLanguageMathTests.cs`:
```csharp
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
```

**Step 2: Run to verify fail**
```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~NaturalLanguageMathTests"
```
Expected: 4 new failures.

**Step 3: Implement**

In `src/Antinote/Commands/NaturalLanguageMath.cs`, add three new `Regex` fields after `NaturalSentence`:
```csharp
private static readonly Regex PowerOp =
    new(@"(\d+(?:\.\d+)?)\s*\^\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);

private static readonly Regex SqrtFix =
    new(@"\bsqrt\s*\(", RegexOptions.IgnoreCase);

private static readonly Regex BarePercent =
    new(@"(\d+(?:\.\d+)?)%");
```

In `Normalize`, add three new pre-processing steps **before** the existing word-number replacement loop:
```csharp
// Pow operator
input = PowerOp.Replace(input, m => $"Pow({m.Groups[1].Value},{m.Groups[2].Value})");

// sqrt case fix
input = SqrtFix.Replace(input, "Sqrt(");

// bare percentage
input = BarePercent.Replace(input, "($1/100)");
```

**Step 4: Run tests to verify pass**
```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~NaturalLanguageMathTests"
```
Expected: all passed (14 total).

**Step 5: Commit**
```bash
git add src/Antinote/Commands/NaturalLanguageMath.cs \
        tests/Antinote.Tests/Commands/NaturalLanguageMathTests.cs
git commit -m "feat: improve NaturalLanguageMath — Pow, sqrt casing, bare percent"
```

---

### Task 5: Popup Positioning Fix

**Files:**
- Modify: `src/Antinote/Commands/SlashCommandEngine.cs`

**Step 1: Fix GetCaretScreenPosition**

In `src/Antinote/Commands/SlashCommandEngine.cs`, replace `GetCaretScreenPosition`:
```csharp
private (double X, double Y) GetCaretScreenPosition()
{
    var pos = _editor.TextArea.TextView.GetVisualPosition(
        new ICSharpCode.AvalonEdit.TextViewPosition(_editor.TextArea.Caret.Line, _editor.TextArea.Caret.Column),
        VisualYPosition.LineBottom);
    var screenPos = _editor.TextArea.TextView.PointToScreen(pos);

    // PointToScreen returns physical pixels; Popup.HorizontalOffset/VerticalOffset
    // uses device-independent units — divide by DPI scale to convert.
    var source = System.Windows.PresentationSource.FromVisual(_editor.TextArea.TextView);
    var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
    var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
    return (screenPos.X / dpiX, screenPos.Y / dpiY);
}
```

**Step 2: Build to verify**
```bash
dotnet build src/Antinote
```
Expected: `Build succeeded.`

**Step 3: Commit**
```bash
git add src/Antinote/Commands/SlashCommandEngine.cs
git commit -m "fix: popup position — divide PointToScreen result by DPI scale"
```

---

### Task 6: UI Tweaks

**Files:**
- Modify: `src/Antinote/Views/NoteWindow.xaml`

**Step 1: Apply all XAML changes**

Replace the full contents of `src/Antinote/Views/NoteWindow.xaml` with:
```xml
<Window x:Class="Antinote.Views.NoteWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonedit="http://icsharpcode.net/sharpdevelop/avalonedit"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ShowInTaskbar="False"
        Topmost="True"
        Width="300" Height="420"
        MinWidth="240" MinHeight="300"
        ResizeMode="CanResizeWithGrip"
        WindowStartupLocation="CenterScreen"
        Opacity="0">

    <Window.Resources>
        <FontFamily x:Key="JetBrainsMono">
            pack://application:,,,/Assets/Fonts/#JetBrains Mono
        </FontFamily>
    </Window.Resources>

    <Border x:Name="MainBorder"
            CornerRadius="16"
            Background="White"
            Margin="12"
            MouseLeftButtonDown="MainBorder_MouseLeftButtonDown">
        <Border.Effect>
            <DropShadowEffect BlurRadius="24" ShadowDepth="4"
                              Direction="270" Color="#333333" Opacity="0.18"/>
        </Border.Effect>

        <Grid>
            <avalonedit:TextEditor
                x:Name="Editor"
                Background="Transparent"
                BorderThickness="0"
                Padding="20,28,20,36"
                FontFamily="{StaticResource JetBrainsMono}"
                FontSize="10"
                Foreground="#333333"
                WordWrap="True"
                ShowLineNumbers="False"
                VerticalScrollBarVisibility="Auto"
                HorizontalScrollBarVisibility="Disabled"/>

            <TextBlock x:Name="Placeholder"
                       Text="Start writing..."
                       FontFamily="{StaticResource JetBrainsMono}"
                       FontSize="10"
                       Foreground="#CCCCCC"
                       Margin="24,28,0,0"
                       VerticalAlignment="Top"
                       IsHitTestVisible="False"/>

            <TextBlock x:Name="DateLabel"
                       FontFamily="{StaticResource JetBrainsMono}"
                       FontSize="9"
                       Foreground="#DDDDDD"
                       HorizontalAlignment="Right"
                       VerticalAlignment="Bottom"
                       Margin="0,0,20,14"
                       IsHitTestVisible="False"/>
        </Grid>
    </Border>
</Window>
```

**Step 2: Build to verify**
```bash
dotnet build src/Antinote
```
Expected: `Build succeeded.`

**Step 3: Run full test suite**
```bash
dotnet test
```
Expected: all tests pass.

**Step 4: Commit**
```bash
git add src/Antinote/Views/NoteWindow.xaml
git commit -m "feat: resize to 300x420, font 10px, padding 20/28, replace #000000 with #333333"
```
