# Antinote Phase 1 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a Windows tray app that shows a floating daily Markdown note on `Ctrl+Alt+N`, auto-saves to `%AppData%\Antinote\notes\YYYY-MM-DD.md`.

**Architecture:** A .NET 8 WPF app running as a background process with a system tray icon. A borderless, transparent-background window hosts a styled `Border` + `RichTextBox`. Global hotkey is registered via Win32 `RegisterHotKey` P/Invoke hooked into the WPF message pump.

**Tech Stack:** .NET 8, WPF, C#, `Hardcodet.NotifyIcon.Wpf` (tray icon), Win32 P/Invoke (hotkey), NUnit + NSubstitute (tests)

---

### Task 1: Project Scaffold

**Files:**
- Create: `Antinote.sln`
- Create: `src/Antinote/Antinote.csproj`
- Create: `tests/Antinote.Tests/Antinote.Tests.csproj`

**Step 1: Create the solution and projects**

Run from `C:\Users\Priyansh\Desktop\Antinote`:
```bash
dotnet new sln -n Antinote
dotnet new wpf -n Antinote -o src/Antinote --framework net8.0-windows
dotnet new nunit -n Antinote.Tests -o tests/Antinote.Tests --framework net8.0-windows
dotnet sln add src/Antinote/Antinote.csproj
dotnet sln add tests/Antinote.Tests/Antinote.Tests.csproj
```

**Step 2: Add NuGet packages**

```bash
dotnet add src/Antinote/Antinote.csproj package Hardcodet.NotifyIcon.Wpf
dotnet add tests/Antinote.Tests/Antinote.Tests.csproj package NSubstitute
dotnet add tests/Antinote.Tests/Antinote.Tests.csproj reference src/Antinote/Antinote.csproj
```

**Step 3: Verify it builds**

```bash
dotnet build
```
Expected: `Build succeeded.`

**Step 4: Commit**

```bash
git init
git add .
git commit -m "chore: scaffold WPF project with test project"
```

---

### Task 2: Storage Layer

**Files:**
- Create: `src/Antinote/Storage/NoteStorage.cs`
- Create: `tests/Antinote.Tests/Storage/NoteStorageTests.cs`

**Step 1: Write the failing tests**

Create `tests/Antinote.Tests/Storage/NoteStorageTests.cs`:
```csharp
using NUnit.Framework;
using Antinote.Storage;

namespace Antinote.Tests.Storage;

[TestFixture]
public class NoteStorageTests
{
    private string _tempDir = null!;
    private NoteStorage _storage = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _storage = new NoteStorage(_tempDir);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_tempDir, recursive: true);

    [Test]
    public void GetTodayFilePath_ReturnsCorrectFormat()
    {
        var expected = Path.Combine(_tempDir, $"{DateTime.Today:yyyy-MM-dd}.md");
        Assert.That(_storage.GetTodayFilePath(), Is.EqualTo(expected));
    }

    [Test]
    public void LoadToday_ReturnsEmpty_WhenFileDoesNotExist()
    {
        Assert.That(_storage.LoadToday(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void Save_And_LoadToday_RoundTrips()
    {
        _storage.Save("# Hello\nThis is a note.");
        Assert.That(_storage.LoadToday(), Is.EqualTo("# Hello\nThis is a note."));
    }

    [Test]
    public void Save_CreatesDirectoryIfMissing()
    {
        var nested = Path.Combine(_tempDir, "sub", "notes");
        var storage = new NoteStorage(nested);
        storage.Save("test");
        Assert.That(File.Exists(storage.GetTodayFilePath()), Is.True);
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~NoteStorageTests"
```
Expected: FAIL — `NoteStorage` not found.

**Step 3: Implement NoteStorage**

Create `src/Antinote/Storage/NoteStorage.cs`:
```csharp
namespace Antinote.Storage;

public class NoteStorage
{
    private readonly string _notesDir;

    public NoteStorage(string notesDir)
    {
        _notesDir = notesDir;
    }

    public NoteStorage() : this(
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Antinote", "notes"))
    { }

    public string GetTodayFilePath() =>
        Path.Combine(_notesDir, $"{DateTime.Today:yyyy-MM-dd}.md");

    public string LoadToday()
    {
        var path = GetTodayFilePath();
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    public void Save(string content)
    {
        Directory.CreateDirectory(_notesDir);
        File.WriteAllText(GetTodayFilePath(), content);
    }

    public void OpenNotesFolder()
    {
        Directory.CreateDirectory(_notesDir);
        System.Diagnostics.Process.Start("explorer.exe", _notesDir);
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~NoteStorageTests"
```
Expected: 4 passed.

**Step 5: Commit**

```bash
git add src/Antinote/Storage/NoteStorage.cs tests/Antinote.Tests/Storage/NoteStorageTests.cs
git commit -m "feat: add NoteStorage with daily .md file read/write"
```

---

### Task 3: Hotkey Manager

**Files:**
- Create: `src/Antinote/Hotkey/HotkeyManager.cs`
- Create: `tests/Antinote.Tests/Hotkey/HotkeyManagerTests.cs`

**Step 1: Write the failing test**

Create `tests/Antinote.Tests/Hotkey/HotkeyManagerTests.cs`:
```csharp
using NUnit.Framework;
using Antinote.Hotkey;

namespace Antinote.Tests.Hotkey;

[TestFixture]
public class HotkeyManagerTests
{
    [Test]
    public void ModifierFlags_CtrlAlt_CorrectValue()
    {
        // MOD_ALT = 0x0001, MOD_CONTROL = 0x0002
        Assert.That(HotkeyManager.MOD_ALT | HotkeyManager.MOD_CONTROL, Is.EqualTo(0x0003));
    }
}
```

**Step 2: Run to verify fail**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~HotkeyManagerTests"
```
Expected: FAIL — `HotkeyManager` not found.

**Step 3: Implement HotkeyManager**

Create `src/Antinote/Hotkey/HotkeyManager.cs`:
```csharp
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
```

**Step 4: Run tests to verify pass**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~HotkeyManagerTests"
```
Expected: 1 passed.

**Step 5: Commit**

```bash
git add src/Antinote/Hotkey/HotkeyManager.cs tests/Antinote.Tests/Hotkey/HotkeyManagerTests.cs
git commit -m "feat: add Win32 global hotkey manager (Ctrl+Alt+N)"
```

---

### Task 4: Note Window UI

**Files:**
- Create: `src/Antinote/Views/NoteWindow.xaml`
- Create: `src/Antinote/Views/NoteWindow.xaml.cs`
- Modify: `src/Antinote/Antinote.csproj` (embed JetBrains Mono font)

**Step 1: Download JetBrains Mono font**

Download `JetBrainsMono-Regular.ttf` from https://www.jetbrains.com/lp/mono/ and place it at:
`src/Antinote/Assets/Fonts/JetBrainsMono-Regular.ttf`

**Step 2: Add font as embedded resource in csproj**

Edit `src/Antinote/Antinote.csproj`, add inside `<Project>`:
```xml
<ItemGroup>
  <Resource Include="Assets\Fonts\JetBrainsMono-Regular.ttf" />
</ItemGroup>
```

**Step 3: Create NoteWindow.xaml**

Create `src/Antinote/Views/NoteWindow.xaml`:
```xml
<Window x:Class="Antinote.Views.NoteWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ShowInTaskbar="False"
        Topmost="True"
        Width="600" Height="400"
        MinWidth="400" MinHeight="250"
        ResizeMode="CanResizeWithGrip"
        WindowStartupLocation="CenterScreen"
        Opacity="0">

    <Window.Resources>
        <FontFamily x:Key="JetBrainsMono">
            pack://application:,,,/Assets/Fonts/#JetBrains Mono
        </FontFamily>
    </Window.Resources>

    <Border CornerRadius="16"
            Background="White"
            Padding="0"
            Margin="12">
        <Border.Effect>
            <DropShadowEffect BlurRadius="24"
                              ShadowDepth="4"
                              Direction="270"
                              Color="#000000"
                              Opacity="0.18"/>
        </Border.Effect>

        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="40"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- Header: date + drag area -->
            <Border Grid.Row="0"
                    Background="Transparent"
                    CornerRadius="16,16,0,0"
                    MouseLeftButtonDown="Header_MouseLeftButtonDown">
                <TextBlock x:Name="DateLabel"
                           VerticalAlignment="Center"
                           HorizontalAlignment="Left"
                           Margin="24,0,0,0"
                           FontFamily="{StaticResource JetBrainsMono}"
                           FontSize="12"
                           Foreground="#AAAAAA"/>
            </Border>

            <!-- Editor -->
            <Grid Grid.Row="1">
                <RichTextBox x:Name="Editor"
                             Background="Transparent"
                             BorderThickness="0"
                             Padding="24,8,24,24"
                             FontFamily="{StaticResource JetBrainsMono}"
                             FontSize="14"
                             Foreground="#1A1A1A"
                             AcceptsReturn="True"
                             AcceptsTab="True"
                             VerticalScrollBarVisibility="Auto"
                             TextChanged="Editor_TextChanged"/>

                <!-- Placeholder -->
                <TextBlock x:Name="Placeholder"
                           Text="Start writing..."
                           FontFamily="{StaticResource JetBrainsMono}"
                           FontSize="14"
                           Foreground="#CCCCCC"
                           Margin="28,8,0,0"
                           VerticalAlignment="Top"
                           IsHitTestVisible="False"/>
            </Grid>
        </Grid>
    </Border>
</Window>
```

**Step 4: Create NoteWindow.xaml.cs**

Create `src/Antinote/Views/NoteWindow.xaml.cs`:
```csharp
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
        // RichTextBox appends \r\n; trim trailing whitespace
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
```

**Step 5: Build to verify no errors**

```bash
dotnet build src/Antinote
```
Expected: `Build succeeded.`

**Step 6: Commit**

```bash
git add src/Antinote/Views/ src/Antinote/Assets/ src/Antinote/Antinote.csproj
git commit -m "feat: add NoteWindow with rounded UI, JetBrains Mono, auto-save debounce"
```

---

### Task 5: System Tray + App Wiring

**Files:**
- Modify: `src/Antinote/App.xaml`
- Modify: `src/Antinote/App.xaml.cs`
- Create: `src/Antinote/Assets/Icons/tray.ico`

**Step 1: Create a tray icon**

Create a simple 16×16 or 32×32 `.ico` file and place it at `src/Antinote/Assets/Icons/tray.ico`. You can use any icon editor or download a free pencil/note icon from https://icons8.com (free for desktop apps with attribution, or use a public domain icon).

Add to `Antinote.csproj`:
```xml
<ItemGroup>
  <Resource Include="Assets\Icons\tray.ico" />
</ItemGroup>
```

**Step 2: Update App.xaml**

Replace `src/Antinote/App.xaml` content:
```xml
<Application x:Class="Antinote.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:tb="http://www.hardcodet.net/taskbar">
    <Application.Resources>
        <tb:TaskbarIcon x:Key="TrayIcon"/>
    </Application.Resources>
</Application>
```

**Step 3: Update App.xaml.cs**

Replace `src/Antinote/App.xaml.cs` content:
```csharp
using System.Windows;
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

        var menu = new System.Windows.Controls.ContextMenu();

        var openItem = new System.Windows.Controls.MenuItem { Header = "Open Today's Note" };
        openItem.Click += (_, _) => _noteWindow!.Show();

        var folderItem = new System.Windows.Controls.MenuItem { Header = "Open Notes Folder" };
        folderItem.Click += (_, _) => _storage.OpenNotesFolder();

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => { _hotkeyManager?.Dispose(); _trayIcon?.Dispose(); Shutdown(); };

        menu.Items.Add(openItem);
        menu.Items.Add(folderItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
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
```

**Step 4: Build and run manually**

```bash
dotnet run --project src/Antinote
```

Expected:
- App appears in system tray
- `Ctrl+Alt+N` shows/hides the white floating note window
- Window is centered, rounded, has today's date in header
- Typing auto-saves after 500ms
- `Escape` or clicking outside hides it
- Right-click tray shows menu

**Step 5: Commit**

```bash
git add src/Antinote/App.xaml src/Antinote/App.xaml.cs src/Antinote/Assets/Icons/
git commit -m "feat: wire up system tray, hotkey toggle, and app lifecycle"
```

---

### Task 6: Windows Startup Registration

**Files:**
- Create: `src/Antinote/Startup/StartupManager.cs`
- Create: `tests/Antinote.Tests/Startup/StartupManagerTests.cs`

**Step 1: Write the failing test**

Create `tests/Antinote.Tests/Startup/StartupManagerTests.cs`:
```csharp
using NUnit.Framework;
using Antinote.Startup;

namespace Antinote.Tests.Startup;

[TestFixture]
public class StartupManagerTests
{
    [Test]
    public void RegistryKeyName_IsCorrect()
    {
        Assert.That(StartupManager.AppName, Is.EqualTo("Antinote"));
    }
}
```

**Step 2: Run to verify fail**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~StartupManagerTests"
```
Expected: FAIL.

**Step 3: Implement StartupManager**

Create `src/Antinote/Startup/StartupManager.cs`:
```csharp
using Microsoft.Win32;

namespace Antinote.Startup;

public static class StartupManager
{
    public const string AppName = "Antinote";
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.SetValue(AppName, $"\"{Environment.ProcessPath}\"");
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppName) != null;
    }
}
```

**Step 4: Call Enable() on first run in App.xaml.cs**

Add to `OnStartup` in `App.xaml.cs` after `base.OnStartup(e)`:
```csharp
Antinote.Startup.StartupManager.Enable();
```

**Step 5: Run tests**

```bash
dotnet test tests/Antinote.Tests --filter "FullyQualifiedName~StartupManagerTests"
```
Expected: 1 passed.

**Step 6: Commit**

```bash
git add src/Antinote/Startup/ tests/Antinote.Tests/Startup/
git commit -m "feat: register app in Windows startup via registry"
```

---

### Task 7: Final Build Verification

**Step 1: Run all tests**

```bash
dotnet test
```
Expected: All tests pass.

**Step 2: Publish self-contained executable**

```bash
dotnet publish src/Antinote -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```
Expected: `publish/Antinote.exe` produced.

**Step 3: Smoke test the published exe**

Run `publish/Antinote.exe` directly. Verify:
- Tray icon appears
- `Ctrl+Alt+N` toggles note window
- Note persists after closing and reopening
- App is in Windows startup (`Task Manager > Startup apps`)

**Step 4: Final commit**

```bash
git add publish/ -n  # don't actually commit the binary
git commit -m "chore: verify release build and smoke test complete"
```
