# Antinote Phase 2 — Slash Commands Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the RichTextBox editor with AvalonEdit and add a slash command system with autocomplete popup, math evaluation, and 9 built-in commands.

**Architecture:** AvalonEdit replaces `RichTextBox` in `NoteWindow`. A `SlashCommandEngine` watches keystrokes and shows a `SlashCommandPopup` near the cursor. Each command is a handler that inserts a Markdown snippet. `MathEvaluator` uses NCalc to evaluate `= expression` lines on Enter.

**Tech Stack:** .NET 8, WPF, AvalonEdit (`AvalonEdit` NuGet), NCalc (`NCalc2` NuGet), NUnit, NSubstitute

---

### Task 1: Add Dependencies + Replace RichTextBox with AvalonEdit

**Files:**
- Modify: `src/Antinote/Antinote.csproj`
- Modify: `src/Antinote/Views/NoteWindow.xaml`
- Modify: `src/Antinote/Views/NoteWindow.xaml.cs`

**Step 1: Add NuGet packages**

```bash
dotnet add src/Antinote/Antinote.csproj package AvalonEdit
dotnet add src/Antinote/Antinote.csproj package NCalc2
```

**Step 2: Update NoteWindow.xaml**

Replace the entire `<Grid Grid.Row="1">` editor section with AvalonEdit. The full updated `NoteWindow.xaml`:

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
        Width="600" Height="400"
        MinWidth="400" MinHeight="250"
        ResizeMode="CanResizeWithGrip"
        WindowStartupLocation="CenterScreen"
        Opacity="0">

    <Window.Resources>
        <FontFamily x:Key="ChivoMono">
            pack://application:,,,/Assets/Fonts/#Chivo Mono
        </FontFamily>
    </Window.Resources>

    <Border CornerRadius="16"
            Background="White"
            Margin="12">
        <Border.Effect>
            <DropShadowEffect BlurRadius="24" ShadowDepth="4"
                              Direction="270" Color="#000000" Opacity="0.18"/>
        </Border.Effect>

        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="40"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <Border Grid.Row="0" Background="Transparent"
                    CornerRadius="16,16,0,0"
                    MouseLeftButtonDown="Header_MouseLeftButtonDown">
                <TextBlock x:Name="DateLabel"
                           VerticalAlignment="Center"
                           HorizontalAlignment="Left"
                           Margin="24,0,0,0"
                           FontFamily="{StaticResource ChivoMono}"
                           FontSize="12"
                           Foreground="#AAAAAA"/>
            </Border>

            <Grid Grid.Row="1">
                <avalonedit:TextEditor
                    x:Name="Editor"
                    Background="Transparent"
                    BorderThickness="0"
                    Padding="24,8,24,24"
                    FontFamily="{StaticResource ChivoMono}"
                    FontSize="14"
                    Foreground="#1A1A1A"
                    WordWrap="True"
                    ShowLineNumbers="False"
                    VerticalScrollBarVisibility="Auto"
                    HorizontalScrollBarVisibility="Disabled"/>

                <TextBlock x:Name="Placeholder"
                           Text="Start writing..."
                           FontFamily="{StaticResource ChivoMono}"
                           FontSize="14"
                           Foreground="#CCCCCC"
                           Margin="28,8,0,0"
                           VerticalAlignment="Top"
                           IsHitTestVisible="False"/>
            </Grid>
        </Grid>
    </Border>
</Window>
```

**Step 3: Download Chivo Mono font**

Download `ChivoMono-Regular.ttf` from https://fonts.google.com/specimen/Chivo+Mono and place at:
`src/Antinote/Assets/Fonts/ChivoMono-Regular.ttf`

Add to `Antinote.csproj` (replace the JetBrains Mono entry):
```xml
<ItemGroup>
  <Resource Include="Assets\Fonts\ChivoMono-Regular.ttf" />
</ItemGroup>
```

**Step 4: Update NoteWindow.xaml.cs to use AvalonEdit**

Replace the full file:

```csharp
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Antinote.Storage;

namespace Antinote.Views;

public partial class NoteWindow : Window
{
    private readonly NoteStorage _storage;
    private readonly DispatcherTimer _saveTimer;

    public NoteWindow(NoteStorage storage)
    {
        InitializeComponent();
        _storage = storage;

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); SaveNote(); };

        DateLabel.Text = DateTime.Today.ToString("dddd, MMMM d");

        Editor.TextChanged += Editor_TextChanged;

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
        Editor.TextArea.Caret.Offset = Editor.Text.Length;
    }

    public new void Hide()
    {
        FadeOut(() => base.Hide());
    }

    private void SaveNote() => _storage.Save(Editor.Text);

    private void Editor_TextChanged(object? sender, EventArgs e)
    {
        UpdatePlaceholder();
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void UpdatePlaceholder() =>
        Placeholder.Visibility = string.IsNullOrWhiteSpace(Editor.Text)
            ? Visibility.Visible : Visibility.Collapsed;

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        DragMove();

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

**Step 5: Build to verify**

```bash
dotnet build src/Antinote
```
Expected: `Build succeeded.`

**Step 6: Commit**

```bash
git add src/Antinote/
git commit -m "feat: replace RichTextBox with AvalonEdit, swap font to Chivo Mono"
```

---

### Task 2: MathEvaluator

**Files:**
- Create: `src/Antinote/Commands/MathEvaluator.cs`
- Create: `tests/Antinote.Tests/Commands/MathEvaluatorTests.cs`

**Step 1: Write failing tests**

Create `tests/Antinote.Tests/Commands/MathEvaluatorTests.cs`:
```csharp
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
        Assert.That(result, Is.EqualTo("= 2 + 2 → 4"));
    }

    [Test]
    public void TryFormatMathLine_AlreadyEvaluated_ReturnsNull()
    {
        Assert.That(_evaluator.TryFormatMathLine("= 2 + 2 → 4"), Is.Null);
    }

    [Test]
    public void TryFormatMathLine_NotMathLine_ReturnsNull()
    {
        Assert.That(_evaluator.TryFormatMathLine("just a note"), Is.Null);
    }
}
```

**Step 2: Run to verify fail**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~MathEvaluatorTests"
```
Expected: FAIL — `MathEvaluator` not found.

**Step 3: Implement MathEvaluator**

Create `src/Antinote/Commands/MathEvaluator.cs`:
```csharp
using NCalc;

namespace Antinote.Commands;

public class MathEvaluator
{
    public string? Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;
        try
        {
            var expr = new Expression(expression);
            var result = expr.Evaluate();
            if (result is double d)
                return Math.Round(d, 2).ToString();
            return result?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public string? TryFormatMathLine(string line)
    {
        if (!line.StartsWith("= ")) return null;
        if (line.Contains(" → ")) return null;

        var expression = line[2..].Trim();
        var result = Evaluate(expression);
        if (result == null) return null;

        return $"{line} → {result}";
    }
}
```

**Step 4: Run tests to verify pass**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~MathEvaluatorTests"
```
Expected: 8 passed.

**Step 5: Commit**

```bash
git add src/Antinote/Commands/MathEvaluator.cs tests/Antinote.Tests/Commands/MathEvaluatorTests.cs
git commit -m "feat: add MathEvaluator with NCalc expression evaluation"
```

---

### Task 3: SlashCommandRegistry

**Files:**
- Create: `src/Antinote/Commands/SlashCommand.cs`
- Create: `src/Antinote/Commands/SlashCommandRegistry.cs`
- Create: `tests/Antinote.Tests/Commands/SlashCommandRegistryTests.cs`

**Step 1: Write failing tests**

Create `tests/Antinote.Tests/Commands/SlashCommandRegistryTests.cs`:
```csharp
using NUnit.Framework;
using Antinote.Commands;

namespace Antinote.Tests.Commands;

[TestFixture]
public class SlashCommandRegistryTests
{
    private SlashCommandRegistry _registry = null!;

    [SetUp]
    public void SetUp() => _registry = new SlashCommandRegistry();

    [Test]
    public void GetAll_ReturnsNineCommands()
    {
        Assert.That(_registry.GetAll().Count, Is.EqualTo(9));
    }

    [Test]
    public void Filter_EmptyQuery_ReturnsAll()
    {
        Assert.That(_registry.Filter("").Count, Is.EqualTo(9));
    }

    [Test]
    public void Filter_PartialMatch_ReturnsMatches()
    {
        var results = _registry.Filter("ma");
        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("math"));
    }

    [Test]
    public void Filter_NoMatch_ReturnsEmpty()
    {
        Assert.That(_registry.Filter("xyz").Count, Is.EqualTo(0));
    }

    [Test]
    public void Filter_CaseInsensitive()
    {
        var results = _registry.Filter("MA");
        Assert.That(results.Count, Is.EqualTo(1));
    }

    [Test]
    public void AllCommands_HaveNonEmptyInsertText()
    {
        foreach (var cmd in _registry.GetAll())
            Assert.That(cmd.InsertText, Is.Not.Empty, $"{cmd.Name} has empty InsertText");
    }
}
```

**Step 2: Run to verify fail**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~SlashCommandRegistryTests"
```
Expected: FAIL.

**Step 3: Implement SlashCommand and SlashCommandRegistry**

Create `src/Antinote/Commands/SlashCommand.cs`:
```csharp
namespace Antinote.Commands;

public record SlashCommand(
    string Name,
    string Description,
    string InsertText,
    int CursorOffsetFromEnd = 0
);
```

Create `src/Antinote/Commands/SlashCommandRegistry.cs`:
```csharp
namespace Antinote.Commands;

public class SlashCommandRegistry
{
    private readonly List<SlashCommand> _commands =
    [
        new("math",      "Evaluate a math expression",  "= ",        cursorOffsetFromEnd: 0),
        new("list",      "Bulleted list item",           "- ",        cursorOffsetFromEnd: 0),
        new("checklist", "Checklist with checkboxes",    "- [ ] ",    cursorOffsetFromEnd: 0),
        new("todo",      "Single to-do item",            "- [ ] ",    cursorOffsetFromEnd: 0),
        new("date",      "Insert today's date",          DateTime.Today.ToString("MMMM d, yyyy"), cursorOffsetFromEnd: 0),
        new("time",      "Insert current time",          DateTime.Now.ToString("HH:mm"),          cursorOffsetFromEnd: 0),
        new("divider",   "Section separator",            "---",       cursorOffsetFromEnd: 0),
        new("code",      "Fenced code block",            "```\n\n```", cursorOffsetFromEnd: 4),
        new("heading",   "Section heading",              "## ",       cursorOffsetFromEnd: 0),
    ];

    public List<SlashCommand> GetAll() => _commands;

    public List<SlashCommand> Filter(string query) =>
        _commands
            .Where(c => c.Name.StartsWith(query.ToLower()))
            .ToList();
}
```

**Step 4: Run tests to verify pass**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~SlashCommandRegistryTests"
```
Expected: 6 passed.

**Step 5: Commit**

```bash
git add src/Antinote/Commands/SlashCommand.cs src/Antinote/Commands/SlashCommandRegistry.cs tests/Antinote.Tests/Commands/SlashCommandRegistryTests.cs
git commit -m "feat: add SlashCommand model and registry with 9 commands"
```

---

### Task 4: SlashCommandPopup UI

**Files:**
- Create: `src/Antinote/Views/SlashCommandPopup.xaml`
- Create: `src/Antinote/Views/SlashCommandPopup.xaml.cs`

**Step 1: Create SlashCommandPopup.xaml**

```xml
<Popup x:Class="Antinote.Views.SlashCommandPopup"
       xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
       xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
       AllowsTransparency="True"
       Placement="Absolute"
       StaysOpen="True">

    <Border Background="White"
            CornerRadius="8"
            Padding="4"
            MinWidth="200"
            MaxHeight="220">
        <Border.Effect>
            <DropShadowEffect BlurRadius="16" ShadowDepth="2"
                              Direction="270" Color="#000000" Opacity="0.15"/>
        </Border.Effect>

        <ListBox x:Name="CommandList"
                 BorderThickness="0"
                 Background="Transparent"
                 ScrollViewer.HorizontalScrollBarVisibility="Disabled"
                 MouseLeftButtonUp="CommandList_MouseLeftButtonUp">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal" Margin="8,4">
                        <TextBlock Text="/"
                                   Foreground="#AAAAAA"
                                   FontFamily="pack://application:,,,/Assets/Fonts/#Chivo Mono"
                                   FontSize="13"/>
                        <TextBlock Text="{Binding Name}"
                                   FontFamily="pack://application:,,,/Assets/Fonts/#Chivo Mono"
                                   FontSize="13"
                                   Foreground="#1A1A1A"
                                   Width="90"/>
                        <TextBlock Text="{Binding Description}"
                                   FontFamily="pack://application:,,,/Assets/Fonts/#Chivo Mono"
                                   FontSize="11"
                                   Foreground="#AAAAAA"/>
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
            <ListBox.ItemContainerStyle>
                <Style TargetType="ListBoxItem">
                    <Setter Property="Padding" Value="0"/>
                    <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
                    <Style.Triggers>
                        <Trigger Property="IsSelected" Value="True">
                            <Setter Property="Background" Value="#F0F0F0"/>
                        </Trigger>
                    </Style.Triggers>
                </Style>
            </ListBox.ItemContainerStyle>
        </ListBox>
    </Border>
</Popup>
```

**Step 2: Create SlashCommandPopup.xaml.cs**

```csharp
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using Antinote.Commands;

namespace Antinote.Views;

public partial class SlashCommandPopup : Popup
{
    public event Action<SlashCommand>? CommandSelected;

    public SlashCommandPopup()
    {
        InitializeComponent();
    }

    public void UpdateCommands(List<SlashCommand> commands)
    {
        CommandList.ItemsSource = commands;
        if (commands.Count > 0)
            CommandList.SelectedIndex = 0;
        IsOpen = commands.Count > 0;
    }

    public void MoveSelectionUp()
    {
        if (CommandList.SelectedIndex > 0)
            CommandList.SelectedIndex--;
    }

    public void MoveSelectionDown()
    {
        if (CommandList.SelectedIndex < CommandList.Items.Count - 1)
            CommandList.SelectedIndex++;
    }

    public void ConfirmSelection()
    {
        if (CommandList.SelectedItem is SlashCommand cmd)
        {
            IsOpen = false;
            CommandSelected?.Invoke(cmd);
        }
    }

    private void CommandList_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CommandList.SelectedItem is SlashCommand)
            ConfirmSelection();
    }
}
```

**Step 3: Build to verify**

```bash
dotnet build src/Antinote
```
Expected: `Build succeeded.`

**Step 4: Commit**

```bash
git add src/Antinote/Views/SlashCommandPopup.xaml src/Antinote/Views/SlashCommandPopup.xaml.cs
git commit -m "feat: add SlashCommandPopup WPF control"
```

---

### Task 5: SlashCommandEngine

**Files:**
- Create: `src/Antinote/Commands/SlashCommandEngine.cs`
- Create: `tests/Antinote.Tests/Commands/SlashCommandEngineTests.cs`

**Step 1: Write failing tests**

Create `tests/Antinote.Tests/Commands/SlashCommandEngineTests.cs`:
```csharp
using NUnit.Framework;
using Antinote.Commands;

namespace Antinote.Tests.Commands;

[TestFixture]
public class SlashCommandEngineTests
{
    private SlashCommandRegistry _registry = null!;

    [SetUp]
    public void SetUp() => _registry = new SlashCommandRegistry();

    [Test]
    public void ExtractSlashQuery_SlashAtStart_ReturnsEmptyQuery()
    {
        var result = SlashCommandEngine.ExtractSlashQuery("/");
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void ExtractSlashQuery_SlashWithText_ReturnsQuery()
    {
        var result = SlashCommandEngine.ExtractSlashQuery("/ma");
        Assert.That(result, Is.EqualTo("ma"));
    }

    [Test]
    public void ExtractSlashQuery_NoSlash_ReturnsNull()
    {
        var result = SlashCommandEngine.ExtractSlashQuery("hello");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ExtractSlashQuery_SlashInMiddle_ReturnsNull()
    {
        var result = SlashCommandEngine.ExtractSlashQuery("hello /world");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ExtractSlashQuery_SlashWithSpace_ReturnsNull()
    {
        var result = SlashCommandEngine.ExtractSlashQuery("/ma th");
        Assert.That(result, Is.Null);
    }
}
```

**Step 2: Run to verify fail**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~SlashCommandEngineTests"
```
Expected: FAIL.

**Step 3: Implement SlashCommandEngine**

Create `src/Antinote/Commands/SlashCommandEngine.cs`:
```csharp
using ICSharpCode.AvalonEdit;

namespace Antinote.Commands;

public class SlashCommandEngine
{
    private readonly TextEditor _editor;
    private readonly SlashCommandRegistry _registry;
    private readonly MathEvaluator _mathEvaluator;

    public event Action<List<SlashCommand>, (double X, double Y)>? SuggestionsChanged;
    public event Action? SuggestionsDismissed;

    public SlashCommandEngine(TextEditor editor, SlashCommandRegistry registry, MathEvaluator mathEvaluator)
    {
        _editor = editor;
        _registry = registry;
        _mathEvaluator = mathEvaluator;
    }

    // Extracts the slash query from the current word being typed.
    // Returns empty string for "/", the query for "/ma", null if no slash word.
    public static string? ExtractSlashQuery(string currentWord)
    {
        if (!currentWord.StartsWith("/")) return null;
        var query = currentWord[1..];
        if (query.Contains(' ')) return null;
        return query;
    }

    public void HandleTextChanged()
    {
        var word = GetCurrentWord();
        var query = ExtractSlashQuery(word);

        if (query == null)
        {
            SuggestionsDismissed?.Invoke();
            return;
        }

        var matches = _registry.Filter(query);
        if (matches.Count == 0)
        {
            SuggestionsDismissed?.Invoke();
            return;
        }

        var pos = GetCaretScreenPosition();
        SuggestionsChanged?.Invoke(matches, pos);
    }

    public void ApplyCommand(SlashCommand command)
    {
        var caretOffset = _editor.CaretOffset;
        var word = GetCurrentWord();
        var lineStart = caretOffset - word.Length;

        // Replace "/command" with the insert text
        _editor.Document.Replace(lineStart, word.Length, command.InsertText);

        // Position cursor
        var newOffset = lineStart + command.InsertText.Length - command.CursorOffsetFromEnd;
        _editor.CaretOffset = newOffset;
        _editor.Focus();
    }

    public void HandleEnterKey()
    {
        var line = _editor.Document.GetLineByOffset(_editor.CaretOffset);
        var lineText = _editor.Document.GetText(line.Offset, line.Length);
        var formatted = _mathEvaluator.TryFormatMathLine(lineText);

        if (formatted != null)
        {
            _editor.Document.Replace(line.Offset, line.Length, formatted);
            _editor.CaretOffset = line.Offset + formatted.Length;
        }

        // Insert newline in both cases
        _editor.Document.Insert(_editor.CaretOffset, Environment.NewLine);
        _editor.CaretOffset += Environment.NewLine.Length;
    }

    private string GetCurrentWord()
    {
        var offset = _editor.CaretOffset;
        var text = _editor.Text;
        var start = offset;
        while (start > 0 && text[start - 1] != ' ' && text[start - 1] != '\n' && text[start - 1] != '\r')
            start--;
        return text[start..offset];
    }

    private (double X, double Y) GetCaretScreenPosition()
    {
        var pos = _editor.TextArea.TextView.GetVisualPosition(
            new ICSharpCode.AvalonEdit.TextViewPosition(_editor.TextArea.Caret.Line, _editor.TextArea.Caret.Column),
            ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineBottom);
        var screenPos = _editor.TextArea.TextView.PointToScreen(pos);
        return (screenPos.X, screenPos.Y);
    }
}
```

**Step 4: Run tests to verify pass**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~SlashCommandEngineTests"
```
Expected: 5 passed.

**Step 5: Commit**

```bash
git add src/Antinote/Commands/SlashCommandEngine.cs tests/Antinote.Tests/Commands/SlashCommandEngineTests.cs
git commit -m "feat: add SlashCommandEngine with query extraction and command application"
```

---

### Task 6: Math Line Colorizer

**Files:**
- Create: `src/Antinote/Rendering/MathLineColorizer.cs`

**Step 1: Implement MathLineColorizer**

Create `src/Antinote/Rendering/MathLineColorizer.cs`:
```csharp
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;

namespace Antinote.Rendering;

public class MathLineColorizer : DocumentColorizingTransformer
{
    private static readonly Brush PrefixBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));

    protected override void ColorizeLine(DocumentLine line)
    {
        var text = CurrentContext.Document.GetText(line);
        if (!text.StartsWith("= ")) return;

        // Dim the "= " prefix (first 2 characters)
        ChangeLinePart(line.Offset, line.Offset + 2, element =>
            element.TextRunProperties.SetForegroundBrush(PrefixBrush));
    }
}
```

**Step 2: Register colorizer in NoteWindow constructor**

In `src/Antinote/Views/NoteWindow.xaml.cs`, add to the constructor after `InitializeComponent()`:
```csharp
Editor.TextArea.TextView.LineTransformers.Add(new Antinote.Rendering.MathLineColorizer());
```

**Step 3: Build to verify**

```bash
dotnet build src/Antinote
```
Expected: `Build succeeded.`

**Step 4: Commit**

```bash
git add src/Antinote/Rendering/MathLineColorizer.cs src/Antinote/Views/NoteWindow.xaml.cs
git commit -m "feat: add MathLineColorizer to dim '= ' prefix on math lines"
```

---

### Task 7: Wire SlashCommandEngine into NoteWindow

**Files:**
- Modify: `src/Antinote/Views/NoteWindow.xaml.cs`

**Step 1: Update NoteWindow.xaml.cs to wire up the full slash command system**

Replace the full file with the final wired version:

```csharp
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Antinote.Commands;
using Antinote.Rendering;
using Antinote.Storage;
using Antinote.Views;

namespace Antinote.Views;

public partial class NoteWindow : Window
{
    private readonly NoteStorage _storage;
    private readonly DispatcherTimer _saveTimer;
    private readonly SlashCommandEngine _slashEngine;
    private readonly SlashCommandPopup _slashPopup;
    private bool _suppressTextChanged;

    public NoteWindow(NoteStorage storage)
    {
        InitializeComponent();
        _storage = storage;

        Editor.TextArea.TextView.LineTransformers.Add(new MathLineColorizer());

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); SaveNote(); };

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

        DateLabel.Text = DateTime.Today.ToString("dddd, MMMM d");

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
        _slashEngine.HandleTextChanged();
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void OnSuggestionsChanged(List<SlashCommand> commands, (double X, double Y) pos)
    {
        _slashPopup.HorizontalOffset = pos.X;
        _slashPopup.VerticalOffset = pos.Y + 4;
        _slashPopup.UpdateCommands(commands);
    }

    private void OnSuggestionsDismissed() => _slashPopup.IsOpen = false;

    private void SaveNote() => _storage.Save(Editor.Text);

    private void UpdatePlaceholder() =>
        Placeholder.Visibility = string.IsNullOrWhiteSpace(Editor.Text)
            ? Visibility.Visible : Visibility.Collapsed;

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        DragMove();

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

**Step 2: Build and run manually**

```bash
dotnet run --project src/Antinote
```

Manual test checklist:
- [ ] `Ctrl+Alt+N` opens the note window
- [ ] Type `/` — popup appears with all 9 commands
- [ ] Type `/ma` — popup narrows to `math`
- [ ] Press `Enter` or `Tab` — `/math` replaced with `= `, cursor ready
- [ ] Type `2 + 2` then `Enter` — line becomes `= 2 + 2 → 4`
- [ ] Type `/list` and confirm — `- ` inserted
- [ ] Type `/checklist` and confirm — `- [ ] ` inserted
- [ ] Type `/date` and confirm — today's date inserted
- [ ] Type `/code` and confirm — cursor lands inside code block
- [ ] `= ` prefix appears in lighter gray color
- [ ] Notes auto-save and persist after hide/show
- [ ] `Escape` dismisses popup without hiding window

**Step 3: Commit**

```bash
git add src/Antinote/Views/NoteWindow.xaml.cs
git commit -m "feat: wire slash command engine, popup, and math evaluation into NoteWindow"
```

---

### Task 8: Run All Tests + Publish

**Step 1: Run full test suite**

```bash
dotnet test
```
Expected: All tests pass.

**Step 2: Publish**

```bash
dotnet publish src/Antinote -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```
Expected: `publish/Antinote.exe` produced.

**Step 3: Final commit**

```bash
git commit -m "chore: phase 2 complete — slash commands, math eval, AvalonEdit"
```
