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
        return mode switch
        {
            DocumentMode.List when lineText == "- "    => null,
            DocumentMode.List when lineText.StartsWith("- ") => "\n- ",
            DocumentMode.Checklist when lineText == "- [ ] " => null,
            DocumentMode.Checklist when lineText.StartsWith("- [ ] ") || lineText.StartsWith("- [x] ") => "\n- [ ] ",
            _ => "\n"
        };
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

        if (mode == DocumentMode.Math)
        {
            var formatted = _mathEvaluator.TryFormatMathLine(lineText);
            if (formatted != null)
            {
                _editor.Document.Replace(line.Offset, line.Length, formatted);
                _editor.CaretOffset = line.Offset + formatted.Length;
            }
            // AvalonEdit stores \n internally — don't use Environment.NewLine
            _editor.Document.Insert(_editor.CaretOffset, "\n");
            _editor.CaretOffset += 1;
            return;
        }

        var continuation = GetEnterContinuation(mode, lineText);
        if (continuation == null)
        {
            _editor.Document.Replace(line.Offset, line.Length, "");
            _editor.CaretOffset = line.Offset;
            _editor.Document.Insert(_editor.CaretOffset, "\n");
            _editor.CaretOffset += 1;
        }
        else
        {
            // continuation already uses \n which matches AvalonEdit's internal storage
            _editor.Document.Insert(_editor.CaretOffset, continuation);
            _editor.CaretOffset += continuation.Length;
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
