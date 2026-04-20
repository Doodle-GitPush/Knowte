using System;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Antinote.Hotkey;
using Antinote.Storage;
using Antinote.Views;

namespace Antinote;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private NoteWindow? _noteWindow;
    private HotkeyManager? _hotkeyManager;
    private readonly NoteStorage _storage = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        Antinote.Startup.StartupManager.Enable();

        _noteWindow = new NoteWindow(_storage);
        _noteWindow.Show();
        _noteWindow.Hide();

        _hotkeyManager = new HotkeyManager(_noteWindow, ToggleNoteWindow);

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
        _trayIcon.ToolTipText = "Antinote";

        var menu = new ContextMenu();

        var openItem = new MenuItem { Header = "Open Today's Note" };
        openItem.Click += (_, _) => _noteWindow!.Show();

        var folderItem = new MenuItem { Header = "Open Notes Folder" };
        folderItem.Click += (_, _) => _storage.OpenNotesFolder();

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => { _hotkeyManager?.Dispose(); _trayIcon?.Dispose(); Shutdown(); };

        menu.Items.Add(openItem);
        menu.Items.Add(folderItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenu = menu;
        _trayIcon.TrayMouseDoubleClick += (_, _) => ToggleNoteWindow();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
