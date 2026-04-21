using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Knowte.Hotkey;

namespace Knowte.Views;

public partial class HotkeyPickerDialog : Window
{
    // The chosen shortcut (set when user presses Save)
    public int ResultModifiers { get; private set; }
    public int ResultVk { get; private set; }

    private int _pendingModifiers;
    private int _pendingVk;
    private string _pendingDisplay = "";
    private bool _hasValidCombo;

    private static readonly SolidColorBrush FocusBorder  = new(Color.FromRgb(0xF9, 0x73, 0x16));
    private static readonly SolidColorBrush NormalBorder = new(Color.FromRgb(0xE0, 0xE0, 0xE0));

    public HotkeyPickerDialog(int currentModifiers, int currentVk)
    {
        InitializeComponent();
        ResultModifiers = currentModifiers;
        ResultVk        = currentVk;
        CurrentLabel.Text = $"Currently: {FormatHotkey(currentModifiers, currentVk)}";
        KeyDown += Window_KeyDown;
    }

    // ── Capture box focus/click ───────────────────────────────────────────────

    private void CaptureBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CaptureBox.Focus();
    }

    private void CaptureBox_GotFocus(object sender, RoutedEventArgs e)
    {
        CaptureBox.BorderBrush = FocusBorder;
        if (!_hasValidCombo)
            CaptureLabel.Text = "Press your shortcut...";
    }

    private void CaptureBox_LostFocus(object sender, RoutedEventArgs e)
    {
        CaptureBox.BorderBrush = NormalBorder;
        if (!_hasValidCombo)
            CaptureLabel.Text = "Click here, then press your shortcut";
    }

    // ── Key capture ───────────────────────────────────────────────────────────

    private void CaptureBox_KeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        HandleKeyCapture(e);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.OriginalSource == CaptureBox) return; // already handled
        if (e.Key == Key.Escape) { Close(); return; }
    }

    private void HandleKeyCapture(KeyEventArgs e)
    {
        // Resolve system key (Alt+X gives Key.System with SystemKey = X)
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Ignore pure modifier presses
        if (key is Key.LeftCtrl or Key.RightCtrl or
                   Key.LeftAlt  or Key.RightAlt  or
                   Key.LeftShift or Key.RightShift or
                   Key.LWin or Key.RWin)
            return;

        int modifiers = 0;
        if (Keyboard.IsKeyDown(Key.LeftCtrl)  || Keyboard.IsKeyDown(Key.RightCtrl))
            modifiers |= HotkeyManager.MOD_CONTROL;
        if (Keyboard.IsKeyDown(Key.LeftAlt)   || Keyboard.IsKeyDown(Key.RightAlt))
            modifiers |= HotkeyManager.MOD_ALT;
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            modifiers |= HotkeyManager.MOD_SHIFT;

        if (modifiers == 0)
        {
            ShowValidation("Shortcut must include Ctrl, Alt, or Shift.");
            return;
        }

        HideValidation();
        _pendingModifiers = modifiers;
        _pendingVk        = KeyInterop.VirtualKeyFromKey(key);
        _pendingDisplay   = FormatHotkey(modifiers, _pendingVk);
        _hasValidCombo    = true;

        CaptureLabel.Text       = _pendingDisplay;
        CaptureLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
        SaveBtn.IsEnabled       = true;
    }

    // ── Buttons ───────────────────────────────────────────────────────────────

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        ResultModifiers = _pendingModifiers;
        ResultVk        = _pendingVk;
        DialogResult    = true;
        Close();
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ShowValidation(string message)
    {
        ValidationLabel.Text       = message;
        ValidationLabel.Visibility = Visibility.Visible;
    }

    private void HideValidation()
    {
        ValidationLabel.Visibility = Visibility.Collapsed;
    }

    public static string FormatHotkey(int modifiers, int vk)
    {
        var parts = new List<string>();
        if ((modifiers & HotkeyManager.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & HotkeyManager.MOD_ALT)     != 0) parts.Add("Alt");
        if ((modifiers & HotkeyManager.MOD_SHIFT)   != 0) parts.Add("Shift");

        var key = KeyInterop.KeyFromVirtualKey(vk);
        parts.Add(key.ToString());
        return string.Join(" + ", parts);
    }
}
