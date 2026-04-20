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
        Editor.CaretOffset = Editor.Text.Length;
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
