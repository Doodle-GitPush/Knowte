using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Antinote.Storage;

namespace Antinote.Views;

public partial class NoteWindow : Window
{
    private readonly NoteStorage _storage;
    private readonly DispatcherTimer _saveTimer;
    private bool _suppressTextChanged;

    public NoteWindow(NoteStorage storage)
    {
        InitializeComponent();
        _storage = storage;

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); SaveNote(); };

        DateLabel.Text = DateTime.Today.ToString("dddd, MMMM d");

        Deactivated += (_, _) => Hide();
        KeyDown += (_, e) => { if (e.Key == Key.Escape) Hide(); };
    }

    public new void Show()
    {
        LoadNote();
        base.Show();
        FadeIn();
        Activate();
        Editor.Focus();
        Editor.CaretPosition = Editor.Document.ContentEnd;
    }

    public new void Hide()
    {
        FadeOut(() => base.Hide());
    }

    private void LoadNote()
    {
        _suppressTextChanged = true;
        var content = _storage.LoadToday();
        var doc = new FlowDocument();
        var para = new Paragraph(new Run(content));
        doc.Blocks.Add(para);
        Editor.Document = doc;
        UpdatePlaceholder();
        _suppressTextChanged = false;
    }

    private void SaveNote()
    {
        var text = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text;
        _storage.Save(text.TrimEnd());
    }

    private void Editor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_suppressTextChanged) return;
        UpdatePlaceholder();
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void UpdatePlaceholder()
    {
        var text = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text;
        Placeholder.Visibility = string.IsNullOrWhiteSpace(text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

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
