using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Knowte.Hotkey;

public class HotkeyManager : IDisposable
{
    public const int MOD_ALT     = 0x0001;
    public const int MOD_CONTROL = 0x0002;
    public const int MOD_SHIFT   = 0x0004;
    public const int MOD_WIN     = 0x0008;

    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId  = 9000;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _source;
    private readonly Action _onTriggered;
    private int _modifiers;
    private int _vk;

    public HotkeyManager(Window owner, Action onTriggered, int modifiers, int vk)
    {
        _onTriggered = onTriggered;
        _modifiers   = modifiers;
        _vk          = vk;

        var helper = new WindowInteropHelper(owner);
        helper.EnsureHandle();
        _source = HwndSource.FromHwnd(helper.Handle);
        _source.AddHook(HwndHook);
        RegisterHotKey(helper.Handle, HotkeyId, _modifiers, _vk);
    }

    /// <summary>Unregisters the old shortcut and registers a new one.</summary>
    public bool Rebind(int modifiers, int vk)
    {
        if (_source == null) return false;
        UnregisterHotKey(_source.Handle, HotkeyId);
        _modifiers = modifiers;
        _vk        = vk;
        return RegisterHotKey(_source.Handle, HotkeyId, _modifiers, _vk);
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
