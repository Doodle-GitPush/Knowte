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

        if (lineText.Contains(" \u2192 ") || string.Equals(lineText, "math", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(lineText))
        {
            _ghostRenderer.SetGhost(null);
            return;
        }

        var normalized = NaturalLanguageMath.Normalize(lineText);
        var result = new MathEvaluator().Evaluate(normalized);

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
