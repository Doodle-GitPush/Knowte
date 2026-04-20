using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Antinote.Hotkey;

public class HotkeyManager : IDisposable
{
    public const int MOD_ALT = 0x0001;
    public const int MOD_CONTROL = 0x0002;
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 9000;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _source;
    private readonly Action _onTriggered;

    public HotkeyManager(Window owner, Action onTriggered)
    {
        _onTriggered = onTriggered;
        var helper = new WindowInteropHelper(owner);
        helper.EnsureHandle();
        _source = HwndSource.FromHwnd(helper.Handle);
        _source.AddHook(HwndHook);
        // Ctrl+Alt+N  (N = 0x4E)
        RegisterHotKey(helper.Handle, HotkeyId, MOD_ALT | MOD_CONTROL, 0x4E);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            _onTriggered();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        _source?.RemoveHook(HwndHook);
        if (_source != null)
            UnregisterHotKey(_source.Handle, HotkeyId);
        _source = null;
    }
}
