using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Antinote.Views;

public partial class TimerWidget : Window
{
    private readonly DispatcherTimer _ticker;
    private TimeSpan _remaining;

    public event Action? TimerFinished;

    public TimerWidget(TimeSpan duration, string label)
    {
        InitializeComponent();
        _remaining = duration;

        // Position: bottom-right of work area, above taskbar
        Loaded += (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - ActualWidth  - 24;
            Top  = area.Bottom - ActualHeight - 24;
            Refresh();
        };

        _ticker = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _ticker.Tick += Ticker_Tick;

        MouseEnter += (_, _) => FadeElement(CloseBtn, 1, 150);
        MouseLeave += (_, _) => FadeElement(CloseBtn, 0, 200);

        Loaded += (_, _) => { FadeIn(); _ticker.Start(); };
    }

    private void Ticker_Tick(object? sender, EventArgs e)
    {
        _remaining -= TimeSpan.FromSeconds(1);
        if (_remaining <= TimeSpan.Zero)
        {
            _remaining = TimeSpan.Zero;
            Refresh();
            _ticker.Stop();
            TimerFinished?.Invoke();
            FadeOut(() => Close());
        }
        else
        {
            Refresh();
        }
    }

    private void Refresh() => TimeLabel.Text = FormatTime(_remaining);

    // Format matches the Figma design: "05:00" (no spaces)
    private static string FormatTime(TimeSpan t) =>
        t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
            : $"{t.Minutes:D2}:{t.Seconds:D2}";

    // ── Drag ──────────────────────────────────────────────────────────────────

    private void RootBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!e.Handled) DragMove();
    }

    // ── Close ─────────────────────────────────────────────────────────────────

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        _ticker.Stop();
        FadeOut(() => Close());
    }

    // ── Animations ────────────────────────────────────────────────────────────

    private void FadeIn()
    {
        var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, anim);
    }

    private void FadeOut(Action onComplete)
    {
        var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180));
        anim.Completed += (_, _) => onComplete();
        BeginAnimation(OpacityProperty, anim);
    }

    private static void FadeElement(UIElement el, double to, int ms) =>
        el.BeginAnimation(OpacityProperty, new DoubleAnimation(to, TimeSpan.FromMilliseconds(ms)));
}
