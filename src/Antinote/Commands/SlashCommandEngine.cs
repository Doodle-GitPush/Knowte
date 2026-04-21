using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;

namespace Antinote.Commands;

public class SlashCommandEngine
{
    private readonly TextEditor _editor;
    private readonly SlashCommandRegistry _registry;
    private readonly MathEvaluator _mathEvaluator;

    public event Action<List<SlashCommand>, (double X, double Y)>? SuggestionsChanged;
    public event Action? SuggestionsDismissed;
    public event Action<TimeSpan, string>? TimerStarted;

    public SlashCommandEngine(TextEditor editor, SlashCommandRegistry registry, MathEvaluator mathEvaluator)
    {
        _editor = editor;
        _registry = registry;
        _mathEvaluator = mathEvaluator;
    }

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

    public static string? ToggleChecklistOnLine(string lineText)
    {
        if (lineText.StartsWith("- [ ] "))
            return "- [x] " + lineText[6..];
        if (lineText.StartsWith("- [x] "))
            return "- [ ] " + lineText[6..];
        return null;
    }

    public static string? GetEnterContinuation(DocumentMode mode, string lineText)
    {
        if (mode.HasFlag(DocumentMode.Checklist))
            return lineText == "- [ ] " ? null : "\n- [ ] ";
        if (mode.HasFlag(DocumentMode.List))
            return lineText == "- " ? null : "\n- ";
        return "\n";
    }

    public void ApplyCommand(SlashCommand command)
    {
        var caretOffset = _editor.CaretOffset;
        var word = GetCurrentWord();
        var lineStart = caretOffset - word.Length;

        if (command.Name == "x")
        {
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

    public void HandleEnterKey(DocumentMode mode)
    {
        var line = _editor.Document.GetLineByOffset(_editor.CaretOffset);
        var lineText = _editor.Document.GetText(line.Offset, line.Length);

        // Math-only Enter: format the line with result (skip if list/checklist handles it)
        if (mode.HasFlag(DocumentMode.Math) && !mode.HasFlag(DocumentMode.List) && !mode.HasFlag(DocumentMode.Checklist))
        {
            var formatted = _mathEvaluator.TryFormatMathLine(lineText);
            if (formatted != null)
            {
                _editor.Document.Replace(line.Offset, line.Length, formatted);
                _editor.CaretOffset = line.Offset + formatted.Length;
            }
            _editor.Document.Insert(_editor.CaretOffset, "\n");
            return;
        }

        // Timer Enter: parse duration and fire event
        if (mode.HasFlag(DocumentMode.Timer))
        {
            var duration = TimerParser.TryParse(lineText);
            if (duration.HasValue)
                TimerStarted?.Invoke(duration.Value, lineText.Trim());
            _editor.Document.Insert(_editor.CaretOffset, "\n");
            return;
        }

        // Convert-only Enter: bake conversion result into the line
        if (mode.HasFlag(DocumentMode.Convert) && !mode.HasFlag(DocumentMode.List) && !mode.HasFlag(DocumentMode.Checklist))
        {
            var result = UnitConverter.TryConvert(lineText);
            if (result != null)
            {
                _editor.Document.Replace(line.Offset, line.Length, lineText + $" \u2192 {result}");
                _editor.CaretOffset = line.Offset + lineText.Length + result.Length + 5;
            }
            _editor.Document.Insert(_editor.CaretOffset, "\n");
            return;
        }

        // Extra blank line gap after the mode declaration line
        if (line.LineNumber == 1 && mode != DocumentMode.None)
        {
            var modePrefix = mode.HasFlag(DocumentMode.Checklist) ? "\n\n- [ ] "
                           : mode.HasFlag(DocumentMode.List) ? "\n\n- "
                           : "\n\n";
            _editor.Document.Insert(_editor.CaretOffset, modePrefix);
            return;
        }

        var continuation = GetEnterContinuation(mode, lineText);
        if (continuation == null)
        {
            _editor.Document.Replace(line.Offset, line.Length, "");
            _editor.CaretOffset = line.Offset;
            _editor.Document.Insert(_editor.CaretOffset, "\n");
            // caret auto-advanced by AvalonEdit
        }
        else
        {
            _editor.Document.Insert(_editor.CaretOffset, continuation);
            // caret auto-advanced by AvalonEdit
        }
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
            VisualYPosition.LineBottom);
        var screenPos = _editor.TextArea.TextView.PointToScreen(pos);

        // PointToScreen returns physical pixels; Popup.HorizontalOffset/VerticalOffset
        // uses device-independent units — divide by DPI scale to convert.
        var source = System.Windows.PresentationSource.FromVisual(_editor.TextArea.TextView);
        var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
        return (screenPos.X / dpiX, screenPos.Y / dpiY);
    }
}
