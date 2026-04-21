using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Knowte.Commands;
using Knowte.Rendering;
using Knowte.Storage;

namespace Knowte.Views;

public partial class NoteWindow : Window
{
    private readonly NoteStorage _storage;
    private readonly DispatcherTimer _saveTimer;
    private readonly DispatcherTimer _clipboardTimer;
    private string _lastClipboardText = "";
    private readonly SlashCommandEngine _slashEngine;
    private readonly SlashCommandPopup _slashPopup;
    private readonly GhostTextRenderer _ghostRenderer;
    private readonly AcceptedGhostColorizer _acceptedColorizer;
    private readonly CheckboxElementGenerator _checkboxGenerator;
    private bool _suppressTextChanged;

    // Multi-note
    private string _currentNoteName;

    // Pin
    private bool _isPinned = false;

    // Control bar
    private readonly DispatcherTimer _controlBarHideTimer;

    // Timer countdown
    private DispatcherTimer? _timerCountdown;
    private TimeSpan _timerRemaining;

    // Horizontal scroll accumulator for 2-finger swipe
    private const int WM_MOUSEHWHEEL = 0x020E;
    private int _hScrollAccumulator = 0;

    public NoteWindow(NoteStorage storage)
    {
        InitializeComponent();
        _storage = storage;
        _currentNoteName = _storage.TodayName;

        // Renderers
        Editor.TextArea.TextView.LineTransformers.Add(new MathLineColorizer());
        Editor.TextArea.TextView.LineTransformers.Add(new ModeTokenColorizer());
        _checkboxGenerator = new CheckboxElementGenerator(Editor.Document);
        Editor.TextArea.TextView.ElementGenerators.Add(_checkboxGenerator);
        _ghostRenderer = new GhostTextRenderer(Editor);
        Editor.TextArea.TextView.BackgroundRenderers.Add(_ghostRenderer);
        _acceptedColorizer = new AcceptedGhostColorizer();
        _ghostRenderer.AcceptedColorizer = _acceptedColorizer;
        Editor.TextArea.TextView.LineTransformers.Add(_acceptedColorizer);

        // Save timer
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); SaveNote(); };

        // Clipboard monitor timer (started/stopped in Show/Hide)
        _clipboardTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _clipboardTimer.Tick += ClipboardTimer_Tick;

        // Control bar hide timer
        _controlBarHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _controlBarHideTimer.Tick += (_, _) =>
        {
            _controlBarHideTimer.Stop();
            ControlBar.IsHitTestVisible = false;
            var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            ControlBar.BeginAnimation(OpacityProperty, anim);
        };

        // Slash commands
        var registry = new SlashCommandRegistry();
        var mathEval = new MathEvaluator();
        _slashEngine = new SlashCommandEngine(Editor, registry, mathEval);
        _slashEngine.SuggestionsChanged += OnSuggestionsChanged;
        _slashEngine.SuggestionsDismissed += OnSuggestionsDismissed;
        _slashEngine.TimerStarted += StartTimer;

        _slashPopup = new SlashCommandPopup();
        _slashPopup.PlacementTarget = Editor;
        _slashPopup.CommandSelected += cmd =>
        {
            _suppressTextChanged = true;
            _slashEngine.ApplyCommand(cmd);
            _suppressTextChanged = false;
        };

        UpdateNoteNameLabel();
        DateLabel.Text = DateTime.Today.ToString("MMM d");

        Editor.TextChanged += Editor_TextChanged;
        Editor.TextArea.PreviewKeyDown += Editor_PreviewKeyDown;

        Deactivated += (_, _) =>
        {
            if (_isPinned || !IsVisible) return;
            Hide();
        };
        KeyDown += (_, e) => { if (e.Key == Key.Escape) Hide(); };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var source = System.Windows.Interop.HwndSource.FromHwnd(
            new System.Windows.Interop.WindowInteropHelper(this).Handle);
        source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_MOUSEHWHEEL)
        {
            int delta = (short)((wParam.ToInt64() >> 16) & 0xFFFF);
            _hScrollAccumulator += delta;
            if (_hScrollAccumulator >= 240)
            {
                _hScrollAccumulator = 0;
                Dispatcher.InvokeAsync(() => NavigateNote(1));
            }
            else if (_hScrollAccumulator <= -240)
            {
                _hScrollAccumulator = 0;
                Dispatcher.InvokeAsync(() => NavigateNote(-1));
            }
        }
        return IntPtr.Zero;
    }

    public new void Show()
    {
        Editor.Text = _storage.Load(_currentNoteName);
        UpdatePlaceholder();
        UpdateWordCount();
        UpdateNoteNameLabel();
        base.Show();
        FadeIn();
        Activate();
        Editor.Focus();
        Editor.CaretOffset = Editor.Text.Length;
        _lastClipboardText = Clipboard.ContainsText() ? Clipboard.GetText().Trim() : "";
        if (!_clipboardTimer.IsEnabled) _clipboardTimer.Start();
    }

    public new void Hide()
    {
        if (!DocumentModeDetector.Detect(Editor.Text).HasFlag(DocumentMode.Paste))
            _clipboardTimer.Stop();
        _slashPopup.IsOpen = false;
        FadeOut(() => base.Hide());
    }

    // --- Multi-note navigation ---

    private void LoadCurrentNote(string name)
    {
        SaveNote();
        _currentNoteName = name;
        _suppressTextChanged = true;
        Editor.Text = _storage.Load(name);
        _suppressTextChanged = false;
        UpdatePlaceholder();
        UpdateWordCount();
        UpdateNoteNameLabel();
        Editor.CaretOffset = Editor.Text.Length;
        Editor.Focus();
        UpdateModeIndicator();
    }

    private void NavigateNote(int direction)
    {
        var names = _storage.GetAllNoteNames();
        if (names.Count == 0) return;
        var idx = names.IndexOf(_currentNoteName);
        if (idx < 0) idx = 0;
        var newIdx = (idx + direction + names.Count) % names.Count;
        if (newIdx == idx) return;
        LoadCurrentNote(names[newIdx]);
    }

    private void UpdateNoteNameLabel()
    {
        var name = _currentNoteName;
        // Try parse as date yyyy-MM-dd
        if (DateTime.TryParseExact(name, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date))
        {
            if (date.Date == DateTime.Today)
                NoteNameLabel.Text = "today";
            else
                NoteNameLabel.Text = date.ToString("MMM d");
        }
        else
        {
            NoteNameLabel.Text = name;
        }
    }

    private void PrevNoteBtn_Click(object sender, RoutedEventArgs e) => NavigateNote(-1);
    private void NextNoteBtn_Click(object sender, RoutedEventArgs e) => NavigateNote(1);

    private void NewNoteBtn_Click(object sender, RoutedEventArgs e)
    {
        var name = _storage.CreateNewNote();
        LoadCurrentNote(name);
    }

    // --- Pin ---

    private void PinBtn_Click(object sender, RoutedEventArgs e)
    {
        _isPinned = !_isPinned;
        PinBtn.Content = _isPinned ? "\u25CF" : "\u25CB";
        PinBtn.Foreground = new SolidColorBrush(_isPinned
            ? Color.FromRgb(0x33, 0x33, 0x33)
            : Color.FromRgb(0xAA, 0xAA, 0xAA));
    }

    // --- Control bar hover ---

    private void MainGrid_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(MainGrid);
        if (pos.Y >= MainGrid.ActualHeight - 50)
            ShowControlBar();
        else
            ScheduleHideControlBar();
    }

    private void ControlBar_MouseEnter(object sender, MouseEventArgs e)
    {
        ShowControlBar();
    }

    private void ControlBar_MouseLeave(object sender, MouseEventArgs e)
    {
        ScheduleHideControlBar();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        ScheduleHideControlBar();
    }

    private void ShowControlBar()
    {
        _controlBarHideTimer.Stop();
        ControlBar.IsHitTestVisible = true;
        var anim = new DoubleAnimation(ControlBar.Opacity, 1, TimeSpan.FromMilliseconds(150));
        ControlBar.BeginAnimation(OpacityProperty, anim);
    }

    private void ScheduleHideControlBar()
    {
        _controlBarHideTimer.Stop();
        _controlBarHideTimer.Start();
    }

    // --- Word count ---

    private void UpdateWordCount()
    {
        var text = Editor.Text.Trim();
        if (string.IsNullOrEmpty(text)) { WordCount.Text = ""; return; }
        var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        WordCount.Text = $"{words}w";
    }

    // --- Timer ---

    private TimerWidget? _timerWidget;

    private void StartTimer(TimeSpan duration, string label)
    {
        // Close any existing widget
        _timerWidget?.Close();
        _timerCountdown?.Stop();

        // Show in-note countdown label
        _timerRemaining = duration;
        TimerLabel.Text = FormatTime(_timerRemaining);
        TimerLabel.Visibility = Visibility.Visible;

        // Launch floating widget
        _timerWidget = new TimerWidget(duration, label);
        _timerWidget.TimerFinished += () =>
        {
            TimerLabel.Visibility = Visibility.Collapsed;
            ((App)Application.Current).ShowTimerNotification($"Timer done: {label}");
        };
        _timerWidget.Show();

        // Sync in-note label with widget ticks
        _timerCountdown = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timerCountdown.Tick += (_, _) =>
        {
            _timerRemaining -= TimeSpan.FromSeconds(1);
            if (_timerRemaining <= TimeSpan.Zero)
            {
                _timerCountdown?.Stop();
                TimerLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                TimerLabel.Text = FormatTime(_timerRemaining);
            }
        };
        _timerCountdown.Start();
    }

    private static string FormatTime(TimeSpan t) =>
        t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
            : $"{t.Minutes}:{t.Seconds:D2}";

    // --- Existing event handlers ---

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
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                Editor.TextArea.PerformTextInput("\n");
            else
                _slashEngine.HandleEnterKey(DocumentModeDetector.Detect(Editor.Text));
            e.Handled = true;
        }
    }

    private void Editor_TextChanged(object? sender, EventArgs e)
    {
        if (_suppressTextChanged) return;
        UpdatePlaceholder();
        UpdateWordCount();
        UpdateGhostText();
        _slashEngine.HandleTextChanged();
        _saveTimer.Stop();
        _saveTimer.Start();
        UpdateModeIndicator();
    }

    private void UpdateGhostText()
    {
        var caretOffset = Editor.CaretOffset;
        if (caretOffset > Editor.Document.TextLength) return;

        var caretLine = Editor.Document.GetLineByOffset(caretOffset);

        // Line 1: suggest ';' after bare mode words
        if (caretLine.LineNumber == 1)
        {
            var line1Text = Editor.Document.GetText(caretLine.Offset, caretLine.Length).TrimEnd();
            var tokens = line1Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool hasBareMode = tokens.Any(t => !t.EndsWith(';') && DocumentModeDetector.IsModeName(t));
            if (hasBareMode)
            {
                _ghostRenderer.SetGhost(";");
                return;
            }
        }

        var mode = DocumentModeDetector.Detect(Editor.Text);

        // Timer ghost text: show parsed duration confirmation
        if (mode.HasFlag(DocumentMode.Timer))
        {
            var timerLineText = Editor.Document.GetText(caretLine.Offset, caretLine.Length).Trim();
            var duration = TimerParser.TryParse(timerLineText);
            if (duration.HasValue)
            {
                _ghostRenderer.SetGhost($" \u2192 {FormatTime(duration.Value)}");
                return;
            }
        }

        // Convert ghost text (works alongside math)
        if (mode.HasFlag(DocumentMode.Convert))
        {
            var cvtText = Editor.Document.GetText(caretLine.Offset, caretLine.Length).Trim();
            var cvtResult = UnitConverter.TryConvert(cvtText);
            if (cvtResult != null)
            {
                _ghostRenderer.SetGhost($" \u2192 {cvtResult}");
                return;
            }
        }

        // Math ghost text
        if (mode.HasFlag(DocumentMode.Math))
        {
            var lineText = Editor.Document.GetText(caretLine.Offset, caretLine.Length).Trim();

            if (lineText.Contains(" \u2192 ") || string.IsNullOrWhiteSpace(lineText)
                || DocumentModeDetector.IsModeName(lineText.TrimEnd(';')))
            {
                _ghostRenderer.SetGhost(null);
                return;
            }

            var normalized = NaturalLanguageMath.Normalize(lineText);
            var result = new MathEvaluator().Evaluate(normalized);
            _ghostRenderer.SetGhost(result != null ? $" \u2192 {result}" : null);
            return;
        }

        _ghostRenderer.SetGhost(null);
    }

    private void OnSuggestionsChanged(List<SlashCommand> commands, (double X, double Y) pos)
    {
        _slashPopup.HorizontalOffset = pos.X;
        _slashPopup.VerticalOffset = pos.Y + 6;
        _slashPopup.UpdateCommands(commands);
    }

    private void OnSuggestionsDismissed() => _slashPopup.IsOpen = false;

    private void ClipboardTimer_Tick(object? sender, EventArgs e)
    {
        if (!DocumentModeDetector.Detect(Editor.Text).HasFlag(DocumentMode.Paste)) return;
        try
        {
            if (!Clipboard.ContainsText()) return;
            var text = Clipboard.GetText().Trim();
            if (string.IsNullOrEmpty(text)) return;
            if (text == _lastClipboardText) return;
            if (text.StartsWith(" \u2192 ")) return;
            _lastClipboardText = text;
            _suppressTextChanged = true;
            var separator = Editor.Text.TrimEnd().Length > "paste".Length ? "\n\n" : "\n";
            Editor.Document.Insert(Editor.Document.TextLength, separator + text);
            Editor.CaretOffset = Editor.Document.TextLength;
            _suppressTextChanged = false;
            SaveNote();
        }
        catch { /* Clipboard access can fail on some systems */ }
    }

    private void SaveNote() => _storage.Save(_currentNoteName, Editor.Text);

    private void UpdateModeIndicator()
    {
        ModeIndicator.Children.Clear();
        var mode = DocumentModeDetector.Detect(Editor.Text);
        var modeColors = new (DocumentMode Mode, string Color, string Label)[]
        {
            (DocumentMode.Math,      "#7C3AED", "M"),
            (DocumentMode.List,      "#059669", "L"),
            (DocumentMode.Checklist, "#059669", "C"),
            (DocumentMode.Convert,   "#0284C7", "~"),
            (DocumentMode.Paste,     "#D97706", "P"),
            (DocumentMode.Timer,     "#F97316", "T"),
        };
        foreach (var (m, color, label) in modeColors)
        {
            if (!mode.HasFlag(m)) continue;
            var dot = new System.Windows.Shapes.Ellipse
            {
                Width = 6, Height = 6,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                Margin = new Thickness(2, 0, 0, 0),
                ToolTip = m.ToString()
            };
            ModeIndicator.Children.Add(dot);
        }
    }

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
