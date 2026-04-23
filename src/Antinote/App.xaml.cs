using System;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Knowte.Hotkey;
using Knowte.Storage;
using Knowte.Theme;
using Knowte.Views;

namespace Knowte;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private NoteWindow? _noteWindow;
    private HotkeyManager? _hotkeyManager;
    private readonly NoteStorage _storage = new();
    private KnowteSettings _settings = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        Knowte.Startup.StartupManager.Enable();

        _settings = SettingsStorage.Load();
        ThemeManager.Apply(_settings.IsDarkMode);

        _noteWindow = new NoteWindow(_storage);
        _noteWindow.Show();
        _noteWindow.Hide();

        _hotkeyManager = new HotkeyManager(_noteWindow, ToggleNoteWindow,
            _settings.HotkeyModifiers, _settings.HotkeyVk);

        SetupTrayIcon();
    }

    private void ToggleNoteWindow()
    {
        if (_noteWindow!.IsVisible)
            _noteWindow.Hide();
        else
            _noteWindow.Show();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.IconSource = new System.Windows.Media.Imaging.BitmapImage(
            new Uri("pack://application:,,,/Assets/Icons/tray.ico"));
        _trayIcon.ToolTipText = "Knowte";

        var menu = new ContextMenu();

        var openItem = new MenuItem { Header = "Open Today's Note" };
        openItem.Click += (_, _) => _noteWindow!.Show();

        var folderItem = new MenuItem { Header = "Open Notes Folder" };
        folderItem.Click += (_, _) => _storage.OpenNotesFolder();

        var darkModeItem = new MenuItem
        {
            Header = _settings.IsDarkMode ? "Switch to Light Mode" : "Switch to Dark Mode"
        };
        darkModeItem.Click += (_, _) =>
        {
            _settings.IsDarkMode = !_settings.IsDarkMode;
            ThemeManager.Apply(_settings.IsDarkMode);
            SettingsStorage.Save(_settings);
            darkModeItem.Header = _settings.IsDarkMode ? "Switch to Light Mode" : "Switch to Dark Mode";
        };

        var shortcutItem = new MenuItem
        {
            Header = $"Change Shortcut  ({HotkeyPickerDialog.FormatHotkey(_settings.HotkeyModifiers, _settings.HotkeyVk)})"
        };
        shortcutItem.Click += (_, _) =>
        {
            var dialog = new HotkeyPickerDialog(_settings.HotkeyModifiers, _settings.HotkeyVk);
            if (dialog.ShowDialog() == true)
            {
                if (_hotkeyManager!.Rebind(dialog.ResultModifiers, dialog.ResultVk))
                {
                    _settings.HotkeyModifiers = dialog.ResultModifiers;
                    _settings.HotkeyVk        = dialog.ResultVk;
                    SettingsStorage.Save(_settings);
                    shortcutItem.Header = $"Change Shortcut  ({HotkeyPickerDialog.FormatHotkey(_settings.HotkeyModifiers, _settings.HotkeyVk)})";
                }
                else
                {
                    MessageBox.Show(
                        "That shortcut is already in use by another app. Please try a different combination.",
                        "Knowte", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        };

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => { _hotkeyManager?.Dispose(); _trayIcon?.Dispose(); Shutdown(); };

        menu.Items.Add(openItem);
        menu.Items.Add(folderItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(darkModeItem);
        menu.Items.Add(shortcutItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenu = menu;
        _trayIcon.TrayMouseDoubleClick += (_, _) => ToggleNoteWindow();
    }

    public void ShowTimerNotification(string message)
    {
        _trayIcon?.ShowBalloonTip("Knowte", message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
