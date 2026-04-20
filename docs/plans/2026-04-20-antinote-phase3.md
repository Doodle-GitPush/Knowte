# Antinote Phase 3 — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add math mode with ghost-text preview, real inline checkboxes, popup animations, and a cleaner UI layout.

**Architecture:** Six focused components added to the existing AvalonEdit renderer pipeline (Option B from design). `MathModeDetector` + `NaturalLanguageMath` drive math mode. `GhostTextRenderer` draws inline previews. `CheckboxElementGenerator` inlines real WPF checkboxes. Popup and window layout are restyled. All wired in `NoteWindow`.

**Tech Stack:** .NET 10, WPF, AvalonEdit 6.3, NCalcSync 5.12, NUnit, NSubstitute

**Design doc:** `docs/plans/2026-04-20-antinote-phase3-design.md`

---

### Task 1: NaturalLanguageMath

**Files:**
- Create: `src/Antinote/Commands/NaturalLanguageMath.cs`
- Create: `tests/Antinote.Tests/Commands/NaturalLanguageMathTests.cs`

**Step 1: Write failing tests**

Create `tests/Antinote.Tests/Commands/NaturalLanguageMathTests.cs`:
```csharp
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
}
```

**Step 2: Run to verify fail**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~NaturalLanguageMathTests"
```
Expected: FAIL — `NaturalLanguageMath` not found.

**Step 3: Implement NaturalLanguageMath**

Create `src/Antinote/Commands/NaturalLanguageMath.cs`:
```csharp
using System.Text.RegularExpressions;

namespace Antinote.Commands;

public static class NaturalLanguageMath
{
    private static readonly Dictionary<string, string> WordNumbers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["one"] = "1", ["two"] = "2", ["three"] = "3", ["four"] = "4", ["five"] = "5",
        ["six"] = "6", ["seven"] = "7", ["eight"] = "8", ["nine"] = "9", ["ten"] = "10"
    };

    private static readonly (Regex Pattern, string Replacement)[] Rules =
    [
        (new Regex(@"\bdivided\s+(?:by|in)\b", RegexOptions.IgnoreCase), "/"),
        (new Regex(@"\bmultiplied\s+by\b", RegexOptions.IgnoreCase), "*"),
        (new Regex(@"\btimes\b", RegexOptions.IgnoreCase), "*"),
        (new Regex(@"\bplus\b", RegexOptions.IgnoreCase), "+"),
        (new Regex(@"\bminus\b", RegexOptions.IgnoreCase), "-"),
        (new Regex(@"\bpercent\s+of\b", RegexOptions.IgnoreCase), "/100 *"),
    ];

    // Matches "number [non-numeric words] operator [non-numeric words] number" patterns
    private static readonly Regex NaturalSentence =
        new(@"(\d+(?:\.\d+)?)\s+\w+\s+(divided\s+(?:by|in)|multiplied\s+by|times|plus|minus|percent\s+of)\s+\w+\s+(\d+(?:\.\d+)?)",
            RegexOptions.IgnoreCase);

    public static string Normalize(string input)
    {
        // First, try to extract natural sentence pattern like "20 cookies divided in 6 people"
        var sentenceMatch = NaturalSentence.Match(input);
        if (sentenceMatch.Success)
        {
            input = $"{sentenceMatch.Groups[1].Value} {sentenceMatch.Groups[2].Value} {sentenceMatch.Groups[3].Value}";
        }

        // Replace word numbers
        foreach (var (word, digit) in WordNumbers)
            input = Regex.Replace(input, $@"\b{word}\b", digit, RegexOptions.IgnoreCase);

        // Apply operator rules
        foreach (var (pattern, replacement) in Rules)
            input = pattern.Replace(input, replacement);

        return input;
    }
}
```

**Step 4: Run tests to verify pass**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~NaturalLanguageMathTests"
```
Expected: 10 passed.

**Step 5: Commit**

```bash
git add src/Antinote/Commands/NaturalLanguageMath.cs tests/Antinote.Tests/Commands/NaturalLanguageMathTests.cs
git commit -m "feat: add NaturalLanguageMath keyword normalizer"
```

---

### Task 2: MathModeDetector

**Files:**
- Create: `src/Antinote/Commands/MathModeDetector.cs`
- Create: `tests/Antinote.Tests/Commands/MathModeDetectorTests.cs`

**Step 1: Write failing tests**

Create `tests/Antinote.Tests/Commands/MathModeDetectorTests.cs`:
```csharp
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
```

**Step 2: Run to verify fail**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~MathModeDetectorTests"
```
Expected: FAIL.

**Step 3: Implement MathModeDetector**

Create `src/Antinote/Commands/MathModeDetector.cs`:
```csharp
namespace Antinote.Commands;

public static class MathModeDetector
{
    public static bool IsActive(string documentText)
    {
        if (string.IsNullOrWhiteSpace(documentText)) return false;
        var firstLine = documentText.Split('\n')[0].Trim();
        return string.Equals(firstLine, "math", StringComparison.OrdinalIgnoreCase);
    }
}
```

**Step 4: Run tests to verify pass**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~MathModeDetectorTests"
```
Expected: 6 passed.

**Step 5: Commit**

```bash
git add src/Antinote/Commands/MathModeDetector.cs tests/Antinote.Tests/Commands/MathModeDetectorTests.cs
git commit -m "feat: add MathModeDetector"
```

---

### Task 3: GhostTextRenderer

**Files:**
- Create: `src/Antinote/Rendering/GhostTextRenderer.cs`

**Step 1: Implement GhostTextRenderer**

Create `src/Antinote/Rendering/GhostTextRenderer.cs`:
```csharp
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;

namespace Antinote.Rendering;

public class GhostTextRenderer : IBackgroundRenderer
{
    private readonly TextEditor _editor;
    private string? _ghostText;
    private double _opacity;
    private DispatcherTimer? _fadeTimer;

    public KnownLayer Layer => KnownLayer.Background;

    public GhostTextRenderer(TextEditor editor)
    {
        _editor = editor;
    }

    public string? GhostText => _ghostText;

    public void SetGhost(string? text)
    {
        if (text == _ghostText) return;
        _ghostText = text;
        _fadeTimer?.Stop();

        if (text != null)
        {
            _opacity = 0;
            double elapsed = 0;
            _fadeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _fadeTimer.Tick += (_, _) =>
            {
                elapsed += 16;
                _opacity = Math.Min(1.0, elapsed / 150.0);
                _editor.TextArea.TextView.InvalidateLayer(Layer);
                if (elapsed >= 150) _fadeTimer!.Stop();
            };
            _fadeTimer.Start();
        }
        else
        {
            _opacity = 0;
            _editor.TextArea.TextView.InvalidateLayer(Layer);
        }
    }

    public void Accept()
    {
        if (_ghostText == null) return;
        var text = _ghostText;
        SetGhost(null);
        _editor.Document.Insert(_editor.CaretOffset, text);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_ghostText == null || _opacity <= 0) return;
        if (!textView.VisualLinesValid) return;

        try
        {
            var caretOffset = _editor.CaretOffset;
            if (caretOffset > _editor.Document.TextLength) return;

            var docLine = _editor.Document.GetLineByOffset(caretOffset);
            var lineEndOffset = docLine.Offset + docLine.Length;

            VisualLine? vLine = null;
            foreach (var vl in textView.VisualLines)
            {
                if (vl.FirstDocumentLine.LineNumber == docLine.LineNumber)
                { vLine = vl; break; }
            }
            if (vLine == null) return;

            var pos = textView.GetVisualPosition(
                new ICSharpCode.AvalonEdit.TextViewPosition(docLine.LineNumber, docLine.Length + 1),
                VisualYPosition.TextTop);

            var alpha = (byte)(_opacity * 200);
            var brush = new SolidColorBrush(Color.FromArgb(alpha, 0xCC, 0xCC, 0xCC));
            brush.Freeze();

            var typeface = new Typeface(_editor.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var pixelsPerDip = VisualTreeHelper.GetDpi(textView).PixelsPerDip;

            var ft = new FormattedText(
                _ghostText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                _editor.FontSize,
                brush,
                pixelsPerDip);

            drawingContext.DrawText(ft, new Point(pos.X, pos.Y));
        }
        catch
        {
            // Suppress rendering errors (e.g. visual lines not yet built)
        }
    }
}
```

**Step 2: Build to verify**

```bash
dotnet build src/Antinote
```
Expected: `Build succeeded.`

**Step 3: Commit**

```bash
git add src/Antinote/Rendering/GhostTextRenderer.cs
git commit -m "feat: add GhostTextRenderer for inline math preview"
```

---

### Task 4: CheckboxElementGenerator

**Files:**
- Create: `src/Antinote/Rendering/CheckboxElementGenerator.cs`

**Step 1: Implement CheckboxElementGenerator**

Create `src/Antinote/Rendering/CheckboxElementGenerator.cs`:
```csharp
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Antinote.Rendering;

public class CheckboxElementGenerator : VisualLineElementGenerator
{
    private const string Unchecked = "- [ ] ";
    private const string Checked   = "- [x] ";
    private readonly TextDocument _document;

    public CheckboxElementGenerator(TextDocument document)
    {
        _document = document;
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        var text = _document.Text;
        var pos = startOffset;

        while (pos < text.Length)
        {
            bool atLineStart = pos == 0 || text[pos - 1] == '\n';
            if (atLineStart && pos + 6 <= text.Length)
            {
                var seg = text.Substring(pos, 6);
                if (seg == Unchecked || seg == Checked)
                    return pos;
            }
            var next = text.IndexOf('\n', pos);
            if (next == -1) break;
            pos = next + 1;
        }
        return -1;
    }

    public override VisualLineElement ConstructElement(int offset)
    {
        if (offset + 6 > _document.TextLength) return null!;
        var seg = _document.GetText(offset, 6);
        if (seg != Unchecked && seg != Checked) return null!;

        bool isChecked = seg == Checked;
        var control = new CheckboxControl(isChecked, () => Toggle(offset));
        return new CheckboxVisualElement(control);
    }

    private void Toggle(int offset)
    {
        if (offset + 6 > _document.TextLength) return;
        var current = _document.GetText(offset, 6);
        _document.Replace(offset, 6, current == Unchecked ? Checked : Unchecked);
    }
}

internal class CheckboxVisualElement : InlineObjectElement
{
    public CheckboxVisualElement(UIElement element) : base(20, element)
    {
        DocumentLength = 6;
    }
}

internal class CheckboxControl : FrameworkElement
{
    private readonly bool _isChecked;
    private double _checkProgress; // 0 = no check, 1 = full check

    private static readonly Pen BorderPen = new(new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), 1.5)
        { LineJoin = PenLineJoin.Round };
    private static readonly Brush CheckedBg = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
    private static readonly Pen CheckPen = new(Brushes.White, 1.5)
        { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };

    static CheckboxControl()
    {
        BorderPen.Freeze();
        CheckedBg.Freeze();
        CheckPen.Freeze();
    }

    public CheckboxControl(bool isChecked, Action toggle)
    {
        _isChecked = isChecked;
        _checkProgress = isChecked ? 1.0 : 0.0;
        Width = 14;
        Height = 14;
        Cursor = Cursors.Hand;
        VerticalAlignment = VerticalAlignment.Center;
        Margin = new Thickness(0, 0, 4, 0);

        MouseLeftButtonDown += (_, e) =>
        {
            toggle();
            e.Handled = true;
        };

        if (isChecked)
            AnimateCheckIn();
    }

    private void AnimateCheckIn()
    {
        _checkProgress = 0;
        var timer = new System.Windows.Threading.DispatcherTimer
            { Interval = TimeSpan.FromMilliseconds(16) };
        double elapsed = 0;
        timer.Tick += (_, _) =>
        {
            elapsed += 16;
            _checkProgress = Math.Min(1.0, elapsed / 120.0);
            InvalidateVisual();
            if (elapsed >= 120) timer.Stop();
        };
        timer.Start();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var rect = new Rect(1, 1, 12, 12);

        if (_isChecked)
        {
            dc.DrawRoundedRectangle(CheckedBg, null, rect, 2, 2);

            if (_checkProgress > 0)
            {
                // Draw checkmark progressively: two segments
                // Segment 1: (3,7) → (5.5,9.5)  length ≈ 3.54
                // Segment 2: (5.5,9.5) → (10,4)  length ≈ 6.73
                // Total ≈ 10.27, split at 34%
                const double seg1End = 0.34;
                var p0 = new Point(3, 7);
                var p1 = new Point(5.5, 9.5);
                var p2 = new Point(10, 4);

                if (_checkProgress <= seg1End)
                {
                    double t = _checkProgress / seg1End;
                    var mid = Lerp(p0, p1, t);
                    dc.DrawLine(CheckPen, p0, mid);
                }
                else
                {
                    dc.DrawLine(CheckPen, p0, p1);
                    double t = (_checkProgress - seg1End) / (1.0 - seg1End);
                    var mid = Lerp(p1, p2, t);
                    dc.DrawLine(CheckPen, p1, mid);
                }
            }
        }
        else
        {
            dc.DrawRoundedRectangle(Brushes.White, BorderPen, rect, 2, 2);
        }
    }

    private static Point Lerp(Point a, Point b, double t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}
```

**Step 2: Build to verify**

```bash
dotnet build src/Antinote
```
Expected: `Build succeeded.`

**Step 3: Commit**

```bash
git add src/Antinote/Rendering/CheckboxElementGenerator.cs
git commit -m "feat: add CheckboxElementGenerator with animated inline checkboxes"
```

---

### Task 5: /x Slash Command

**Files:**
- Modify: `src/Antinote/Commands/SlashCommandRegistry.cs`
- Modify: `src/Antinote/Commands/SlashCommandEngine.cs`
- Create: `tests/Antinote.Tests/Commands/SlashCommandXTests.cs`

**Step 1: Write failing test**

Create `tests/Antinote.Tests/Commands/SlashCommandXTests.cs`:
```csharp
using NUnit.Framework;
using Antinote.Commands;

namespace Antinote.Tests.Commands;

[TestFixture]
public class SlashCommandXTests
{
    [Test]
    public void ToggleChecklistLine_Unchecked_BecomesChecked()
    {
        var result = SlashCommandEngine.ToggleChecklistOnLine("- [ ] Buy milk");
        Assert.That(result, Is.EqualTo("- [x] Buy milk"));
    }

    [Test]
    public void ToggleChecklistLine_Checked_BecomesUnchecked()
    {
        var result = SlashCommandEngine.ToggleChecklistOnLine("- [x] Buy milk");
        Assert.That(result, Is.EqualTo("- [ ] Buy milk"));
    }

    [Test]
    public void ToggleChecklistLine_NoCheckbox_ReturnsNull()
    {
        var result = SlashCommandEngine.ToggleChecklistOnLine("just a note");
        Assert.That(result, Is.Null);
    }
}
```

**Step 2: Run to verify fail**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~SlashCommandXTests"
```
Expected: FAIL.

**Step 3: Add /x to registry**

In `src/Antinote/Commands/SlashCommandRegistry.cs`, add one line to `_commands`:
```csharp
new("x", "Mark task done", ""),
```
Add it after the `todo` entry.

**Step 4: Add ToggleChecklistOnLine to SlashCommandEngine**

In `src/Antinote/Commands/SlashCommandEngine.cs`, add this public static method:
```csharp
public static string? ToggleChecklistOnLine(string lineText)
{
    if (lineText.StartsWith("- [ ] "))
        return "- [x] " + lineText[6..];
    if (lineText.StartsWith("- [x] "))
        return "- [ ] " + lineText[6..];
    return null;
}
```

Also update `ApplyCommand` to handle the `x` command specially. Replace the existing `ApplyCommand` with:
```csharp
public void ApplyCommand(SlashCommand command)
{
    var caretOffset = _editor.CaretOffset;
    var word = GetCurrentWord();
    var lineStart = caretOffset - word.Length;

    if (command.Name == "x")
    {
        // Remove the "/x" word, then toggle the checkbox on this line
        _editor.Document.Replace(lineStart, word.Length, "");
        var line = _editor.Document.GetLineByOffset(lineStart);
        var lineText = _editor.Document.GetText(line.Offset, line.Length);
        var toggled = ToggleChecklistOnLine(lineText);
        if (toggled != null)
            _editor.Document.Replace(line.Offset, line.Length, toggled);
        _editor.CaretOffset = line.Offset + (toggled ?? lineText).Length;
        _editor.Focus();
        return;
    }

    _editor.Document.Replace(lineStart, word.Length, command.InsertText);
    var newOffset = lineStart + command.InsertText.Length - command.CursorOffsetFromEnd;
    _editor.CaretOffset = newOffset;
    _editor.Focus();
}
```

**Step 5: Run tests to verify pass**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~SlashCommandXTests"
```
Expected: 3 passed.

**Step 6: Build to verify**

```bash
dotnet build src/Antinote
```
Expected: `Build succeeded.`

**Step 7: Commit**

```bash
git add src/Antinote/Commands/SlashCommandRegistry.cs src/Antinote/Commands/SlashCommandEngine.cs tests/Antinote.Tests/Commands/SlashCommandXTests.cs
git commit -m "feat: add /x command to toggle checklist items"
```

---

### Task 6: Slash Popup Redesign + Animation

**Files:**
- Modify: `src/Antinote/Views/SlashCommandPopup.xaml`
- Modify: `src/Antinote/Views/SlashCommandPopup.xaml.cs`

**Step 1: Replace SlashCommandPopup.xaml**

```xml
<Popup x:Class="Antinote.Views.SlashCommandPopup"
       xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
       xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
       AllowsTransparency="True"
       Placement="Absolute"
       StaysOpen="True">

    <Border x:Name="PopupBorder"
            Background="White"
            CornerRadius="10"
            Padding="4"
            MinWidth="220"
            Opacity="0"
            RenderTransformOrigin="0.5,0">
        <Border.RenderTransform>
            <TranslateTransform x:Name="SlideTransform" Y="8"/>
        </Border.RenderTransform>
        <Border.Effect>
            <DropShadowEffect BlurRadius="12" ShadowDepth="2"
                              Direction="270" Color="#000000" Opacity="0.10"/>
        </Border.Effect>

        <ItemsControl x:Name="CommandList"
                      ScrollViewer.HorizontalScrollBarVisibility="Disabled"
                      ScrollViewer.VerticalScrollBarVisibility="Disabled">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Border x:Name="ItemBorder"
                            CornerRadius="6"
                            Padding="10,6"
                            Background="Transparent">
                        <StackPanel Orientation="Horizontal">
                            <TextBlock FontFamily="pack://application:,,,/Assets/Fonts/#JetBrains Mono"
                                       FontSize="13" Foreground="#AAAAAA" Text="/"/>
                            <TextBlock FontFamily="pack://application:,,,/Assets/Fonts/#JetBrains Mono"
                                       FontSize="13" Foreground="#1A1A1A"
                                       Text="{Binding Name}" MinWidth="80"/>
                            <TextBlock FontFamily="pack://application:,,,/Assets/Fonts/#JetBrains Mono"
                                       FontSize="11" Foreground="#BBBBBB"
                                       Text="{Binding Description}"
                                       VerticalAlignment="Center"/>
                        </StackPanel>
                        <Border.Style>
                            <Style TargetType="Border">
                                <Style.Triggers>
                                    <Trigger Property="IsMouseOver" Value="True">
                                        <Setter Property="Background" Value="#F5F5F5"/>
                                    </Trigger>
                                </Style.Triggers>
                            </Style>
                        </Border.Style>
                    </Border>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </Border>
</Popup>
```

**Step 2: Replace SlashCommandPopup.xaml.cs**

```csharp
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using Antinote.Commands;

namespace Antinote.Views;

public partial class SlashCommandPopup : Popup
{
    public event Action<SlashCommand>? CommandSelected;

    private List<SlashCommand> _commands = [];
    private int _selectedIndex = -1;

    public SlashCommandPopup()
    {
        InitializeComponent();
        CommandList.MouseLeftButtonUp += (_, _) => ConfirmSelection();
    }

    public void UpdateCommands(List<SlashCommand> commands)
    {
        _commands = commands;
        _selectedIndex = commands.Count > 0 ? 0 : -1;
        CommandList.ItemsSource = commands;
        RefreshSelection();

        if (commands.Count > 0 && !IsOpen)
            AnimateOpen();
        else if (commands.Count == 0 && IsOpen)
            AnimateClose();
    }

    public void MoveSelectionUp()
    {
        if (_selectedIndex > 0) { _selectedIndex--; RefreshSelection(); }
    }

    public void MoveSelectionDown()
    {
        if (_selectedIndex < _commands.Count - 1) { _selectedIndex++; RefreshSelection(); }
    }

    public void ConfirmSelection()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _commands.Count) return;
        var cmd = _commands[_selectedIndex];
        AnimateClose(() => CommandSelected?.Invoke(cmd));
    }

    public new bool IsOpen
    {
        get => base.IsOpen;
        set { if (!value && base.IsOpen) AnimateClose(); else base.IsOpen = value; }
    }

    private void AnimateOpen()
    {
        base.IsOpen = true;
        var duration = new Duration(TimeSpan.FromMilliseconds(120));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var opacityAnim = new DoubleAnimation(0, 1, duration) { EasingFunction = ease };
        var slideAnim = new DoubleAnimation(8, 0, duration) { EasingFunction = ease };

        PopupBorder.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
        SlideTransform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
    }

    private void AnimateClose(Action? onComplete = null)
    {
        var duration = new Duration(TimeSpan.FromMilliseconds(80));
        var opacityAnim = new DoubleAnimation(1, 0, duration);
        var slideAnim = new DoubleAnimation(0, 8, duration);

        opacityAnim.Completed += (_, _) =>
        {
            base.IsOpen = false;
            onComplete?.Invoke();
        };

        PopupBorder.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
        SlideTransform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
    }

    private void RefreshSelection()
    {
        // Highlight selected item by updating background via tag
        var items = CommandList.ItemContainerGenerator;
        // ItemsControl doesn't have selection built-in; we track via _selectedIndex
        // Re-bind so DataTemplate can pick it up — simplest: use a SelectedItem overlay
        CommandList.ItemsSource = null;
        CommandList.ItemsSource = _commands;
    }
}
```

> **Note:** `ItemsControl` has no built-in selection. For keyboard selection highlighting, replace `ItemsControl` with `ListBox` and use `ListBox.SelectedIndex`. Update the XAML: change `ItemsControl` to `ListBox` and add `ListBox.ItemContainerStyle` with `IsSelected` trigger for `#F0F0F0` background, and hide the selection border/outline. Here's the adjusted key XAML section:

```xml
<ListBox x:Name="CommandList"
         BorderThickness="0"
         Background="Transparent"
         ScrollViewer.HorizontalScrollBarVisibility="Disabled"
         ScrollViewer.VerticalScrollBarVisibility="Disabled"
         MouseLeftButtonUp="CommandList_MouseLeftButtonUp">
    <ListBox.ItemContainerStyle>
        <Style TargetType="ListBoxItem">
            <Setter Property="Padding" Value="0"/>
            <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="ListBoxItem">
                        <Border x:Name="Bd" CornerRadius="6" Padding="10,6"
                                Background="{TemplateBinding Background}">
                            <ContentPresenter/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsSelected" Value="True">
                                <Setter TargetName="Bd" Property="Background" Value="#F0F0F0"/>
                            </Trigger>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="Bd" Property="Background" Value="#F5F5F5"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </ListBox.ItemContainerStyle>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <TextBlock FontFamily="pack://application:,,,/Assets/Fonts/#JetBrains Mono"
                           FontSize="13" Foreground="#AAAAAA" Text="/"/>
                <TextBlock FontFamily="pack://application:,,,/Assets/Fonts/#JetBrains Mono"
                           FontSize="13" Foreground="#1A1A1A"
                           Text="{Binding Name}" MinWidth="80"/>
                <TextBlock FontFamily="pack://application:,,,/Assets/Fonts/#JetBrains Mono"
                           FontSize="11" Foreground="#BBBBBB"
                           Text="{Binding Description}"
                           VerticalAlignment="Center"/>
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

And update code-behind to use `CommandList.SelectedIndex` for navigation and `CommandList.SelectedItem` for confirmation (same as original). Remove the `RefreshSelection` method and `_selectedIndex` field — `ListBox` handles selection natively.

**Step 3: Build to verify**

```bash
dotnet build src/Antinote
```
Expected: `Build succeeded.`

**Step 4: Commit**

```bash
git add src/Antinote/Views/SlashCommandPopup.xaml src/Antinote/Views/SlashCommandPopup.xaml.cs
git commit -m "feat: redesign slash popup with rounded items, hover states, and slide animation"
```

---

### Task 7: UI Layout (Window size, date, no header)

**Files:**
- Modify: `src/Antinote/Views/NoteWindow.xaml`
- Modify: `src/Antinote/Views/NoteWindow.xaml.cs`

**Step 1: Replace NoteWindow.xaml**

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
        Width="450" Height="630"
        MinWidth="300" MinHeight="400"
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
                              Direction="270" Color="#000000" Opacity="0.18"/>
        </Border.Effect>

        <Grid>
            <avalonedit:TextEditor
                x:Name="Editor"
                Background="Transparent"
                BorderThickness="0"
                Padding="24,20,24,36"
                FontFamily="{StaticResource JetBrainsMono}"
                FontSize="14"
                Foreground="#1A1A1A"
                WordWrap="True"
                ShowLineNumbers="False"
                VerticalScrollBarVisibility="Auto"
                HorizontalScrollBarVisibility="Disabled"/>

            <TextBlock x:Name="Placeholder"
                       Text="Start writing..."
                       FontFamily="{StaticResource JetBrainsMono}"
                       FontSize="14"
                       Foreground="#CCCCCC"
                       Margin="28,20,0,0"
                       VerticalAlignment="Top"
                       IsHitTestVisible="False"/>

            <TextBlock x:Name="DateLabel"
                       FontFamily="{StaticResource JetBrainsMono}"
                       FontSize="10"
                       Foreground="#DDDDDD"
                       HorizontalAlignment="Right"
                       VerticalAlignment="Bottom"
                       Margin="0,0,20,14"
                       IsHitTestVisible="False"/>
        </Grid>
    </Border>
</Window>
```

**Step 2: Update NoteWindow.xaml.cs — set DateLabel in Show()**

Find the line:
```csharp
DateLabel.Text = DateTime.Today.ToString("dddd, MMMM d");
```
Replace with:
```csharp
DateLabel.Text = DateTime.Today.ToString("MMM d");
```

Also update `Header_MouseLeftButtonDown` → `MainBorder_MouseLeftButtonDown`:

Remove:
```csharp
private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
    DragMove();
```

Add:
```csharp
private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    if (!e.Handled) DragMove();
}
```

**Step 3: Build to verify**

```bash
dotnet build src/Antinote
```
Expected: `Build succeeded.`

**Step 4: Commit**

```bash
git add src/Antinote/Views/NoteWindow.xaml src/Antinote/Views/NoteWindow.xaml.cs
git commit -m "feat: resize window to 450x630, move date to bottom-right, remove header"
```

---

### Task 8: Wire Everything into NoteWindow

**Files:**
- Modify: `src/Antinote/Views/NoteWindow.xaml.cs`

**Step 1: Replace the full NoteWindow.xaml.cs**

```csharp
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Antinote.Commands;
using Antinote.Rendering;
using Antinote.Storage;

namespace Antinote.Views;

public partial class NoteWindow : Window
{
    private readonly NoteStorage _storage;
    private readonly DispatcherTimer _saveTimer;
    private readonly SlashCommandEngine _slashEngine;
    private readonly SlashCommandPopup _slashPopup;
    private readonly GhostTextRenderer _ghostRenderer;
    private readonly CheckboxElementGenerator _checkboxGenerator;
    private bool _suppressTextChanged;

    public NoteWindow(NoteStorage storage)
    {
        InitializeComponent();
        _storage = storage;

        // Renderers
        Editor.TextArea.TextView.LineTransformers.Add(new MathLineColorizer());
        _checkboxGenerator = new CheckboxElementGenerator(Editor.Document);
        Editor.TextArea.TextView.ElementGenerators.Add(_checkboxGenerator);
        _ghostRenderer = new GhostTextRenderer(Editor);
        Editor.TextArea.TextView.BackgroundRenderers.Add(_ghostRenderer);

        // Save timer
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); SaveNote(); };

        // Slash commands
        var registry = new SlashCommandRegistry();
        var mathEval = new MathEvaluator();
        _slashEngine = new SlashCommandEngine(Editor, registry, mathEval);
        _slashEngine.SuggestionsChanged += OnSuggestionsChanged;
        _slashEngine.SuggestionsDismissed += OnSuggestionsDismissed;

        _slashPopup = new SlashCommandPopup();
        _slashPopup.PlacementTarget = Editor;
        _slashPopup.CommandSelected += cmd =>
        {
            _suppressTextChanged = true;
            _slashEngine.ApplyCommand(cmd);
            _suppressTextChanged = false;
        };

        DateLabel.Text = DateTime.Today.ToString("MMM d");

        Editor.TextChanged += Editor_TextChanged;
        Editor.TextArea.PreviewKeyDown += Editor_PreviewKeyDown;

        Deactivated += (_, _) => Hide();
        KeyDown += (_, e) => { if (e.Key == Key.Escape) Hide(); };
    }

    public new void Show()
    {
        Editor.Text = _storage.LoadToday();
        UpdatePlaceholder();
        base.Show();
        FadeIn();
        Activate();
        Editor.Focus();
        Editor.CaretOffset = Editor.Text.Length;
    }

    public new void Hide()
    {
        _slashPopup.IsOpen = false;
        FadeOut(() => base.Hide());
    }

    private void Editor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ghost text: Tab accepts
        if (e.Key == Key.Tab && _ghostRenderer.GhostText != null && !_slashPopup.IsOpen)
        {
            _ghostRenderer.Accept();
            e.Handled = true;
            return;
        }

        // Ghost text: any non-modifier key dismisses
        if (_ghostRenderer.GhostText != null && e.Key != Key.LeftShift && e.Key != Key.RightShift
            && e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl && e.Key != Key.LeftAlt && e.Key != Key.RightAlt)
        {
            _ghostRenderer.SetGhost(null);
        }

        if (_slashPopup.IsOpen)
        {
            switch (e.Key)
            {
                case Key.Up:
                    _slashPopup.MoveSelectionUp();
                    e.Handled = true;
                    return;
                case Key.Down:
                    _slashPopup.MoveSelectionDown();
                    e.Handled = true;
                    return;
                case Key.Enter:
                case Key.Tab:
                    _slashPopup.ConfirmSelection();
                    e.Handled = true;
                    return;
                case Key.Escape:
                    _slashPopup.IsOpen = false;
                    e.Handled = true;
                    return;
            }
        }

        if (e.Key == Key.Enter && !_slashPopup.IsOpen)
        {
            _slashEngine.HandleEnterKey();
            e.Handled = true;
        }
    }

    private void Editor_TextChanged(object? sender, EventArgs e)
    {
        if (_suppressTextChanged) return;
        UpdatePlaceholder();
        UpdateGhostText();
        _slashEngine.HandleTextChanged();
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void UpdateGhostText()
    {
        if (!MathModeDetector.IsActive(Editor.Text))
        {
            _ghostRenderer.SetGhost(null);
            return;
        }

        var caretOffset = Editor.CaretOffset;
        if (caretOffset > Editor.Document.TextLength) return;

        var line = Editor.Document.GetLineByOffset(caretOffset);
        var lineText = Editor.Document.GetText(line.Offset, line.Length).Trim();

        // Don't show ghost if line already evaluated or is the "math" keyword line
        if (lineText.Contains(" \u2192 ") || string.Equals(lineText, "math", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(lineText))
        {
            _ghostRenderer.SetGhost(null);
            return;
        }

        var normalized = NaturalLanguageMath.Normalize(lineText);
        var evaluator = new MathEvaluator();
        var result = evaluator.Evaluate(normalized);

        _ghostRenderer.SetGhost(result != null ? $" \u2192 {result}" : null);
    }

    private void OnSuggestionsChanged(List<SlashCommand> commands, (double X, double Y) pos)
    {
        _slashPopup.HorizontalOffset = pos.X;
        _slashPopup.VerticalOffset = pos.Y + 6;
        _slashPopup.UpdateCommands(commands);
    }

    private void OnSuggestionsDismissed() => _slashPopup.IsOpen = false;

    private void SaveNote() => _storage.Save(Editor.Text);

    private void UpdatePlaceholder() =>
        Placeholder.Visibility = string.IsNullOrWhiteSpace(Editor.Text)
            ? Visibility.Visible : Visibility.Collapsed;

    private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!e.Handled) DragMove();
    }

    private void FadeIn()
    {
        var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
        BeginAnimation(OpacityProperty, anim);
    }

    private void FadeOut(Action onComplete)
    {
        var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
        anim.Completed += (_, _) => onComplete();
        BeginAnimation(OpacityProperty, anim);
    }
}
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
Expected: All tests pass.

**Step 4: Commit**

```bash
git add src/Antinote/Views/NoteWindow.xaml.cs
git commit -m "feat: wire math mode, ghost text, checkboxes into NoteWindow"
```

---

### Task 9: Smoke Test + Final Commit

**Manual test checklist:**
- [ ] Window opens at 450×630 via `Ctrl+Alt+N`
- [ ] Date shown at bottom-right in small text
- [ ] Type `math` on line 1, press Enter, type `10 / 3` — ghost text `→ 3.33` fades in
- [ ] Press Tab — ghost text accepted, line becomes `10 / 3 → 3.33`
- [ ] Type `20 cookies divided in 6 people` — ghost text `→ 3.33` appears
- [ ] Press Enter — line formatted with `→ 3.33`
- [ ] Type `/checklist` + Enter — `- [ ] ` with real checkbox inserted
- [ ] Click checkbox — check animates in, text becomes `- [x] `
- [ ] Type `/x` at end of checklist line — item marked done
- [ ] Type `/` — popup appears with fade + slide-up animation
- [ ] Navigate with arrow keys — selection highlights with rounded background
- [ ] Dismiss popup — fade + slide-down animation
- [ ] Escape dismisses popup without closing window
- [ ] Notes persist after hide/show

**Final commit:**

```bash
git add -A
git commit -m "chore: phase 3 complete — math mode, checkboxes, animations, UI redesign"
```
