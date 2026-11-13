# EchoMacro

A small always-on-top Windows toolbar that records global mouse and keyboard input and replays it.

Built with WPF on .NET 8.

## Usage

| Control | What it does |
|---|---|
| **Record** (red dot) | Starts or stops recording. Starting a new recording replaces the current one. |
| **Play** (blue triangle) | Replays the current recording, or stops a running playback. Disabled until something is recorded or loaded. |
| **Repeat** (loop icon) | Repeats the recording until stopped. |
| **Delay (ms)** | Slides out when the pointer is on the repeat icon. Extra wait added before every action. Any value above 0 also slows mouse movement down to a more human-like pace. |
| Right-click the toolbar | Load / Save As / Save a recording, Settings, minimize, close. |
| Drag the toolbar | Moves it. |
| Minimize (right-click menu) | Hides the toolbar into the notification area. Hotkeys keep working; click the tray icon to bring it back. |

### Hotkeys

| Hotkey | Action |
|---|---|
| `Ctrl+Alt+R` | Start / stop recording |
| `Ctrl+Alt+P` | Play / stop playback |
| `Ctrl+Alt+Q` | Close the application |

These are the defaults; change them under **Settings** in the right-click menu. The hotkeys are global, so
each needs at least one of Ctrl, Alt, Shift or Win. If another application already owns one, EchoMacro says
so at startup and that hotkey does not work until a different one is chosen.

## What is recorded

- Key presses and releases, so key combinations (`Ctrl+C`, `Alt+Tab`, ...) are replayed as recorded.
- Mouse button presses and releases for the left, right, middle and both side buttons, including drags.
- Mouse wheel scrolling.

Not recorded:

- The path of the mouse while no button is held. Playback moves the cursor along a generated curve between
  recorded positions. The path of a drag is recorded and replayed as it was.
- Clicks on EchoMacro itself.

Recording and playback are mutually exclusive.

## Things that happen on their own

- **Taking the mouse back stops playback.** Moving the real mouse for two seconds without a break stops a
  running playback. A bump or a short nudge does not. The time can be changed, or the feature turned off,
  in Settings.
- **Unsaved recordings are backed up.** A recording that was never saved is written to the backups folder
  when it is about to be replaced (new recording, loading a file) or when the app closes. The 10 most
  recent backups are kept. There is no prompt.
- **Settings are remembered.** Delay, repeat and the toolbar position are restored on the next start.

## Where files go

EchoMacro keeps its own files in one folder next to the executable:

```
EchoMacroData/
├── settings.json
├── Recordings/     where Load and Save As start
└── Backups/        automatic copies of unsaved recordings
```

Both folders can be pointed elsewhere, and opened, from Settings. If the folder next to the executable is
not writable, `%LocalAppData%\EchoMacro` is used instead.

## Recording files

Recordings are saved as JSON: a list of actions, each with a type, a timestamp in milliseconds and the
key or mouse data. Files written by older versions (single-action clicks and key presses) still load.

Two things to keep in mind:

- A recording is **plain text**. Anything typed while recording, passwords included, ends up in the file.
- Playing a recording **sends real input to your machine**. Only load files you trust.

## Limitations

- Input cannot be sent to windows running as administrator unless EchoMacro runs as administrator too.
- Coordinates are absolute screen positions; a recording depends on the screen layout it was made on.

## Build

```
dotnet build EchoMacro.sln -c Release
```

## Project layout

```
EchoMacro/
├── MainWindow.xaml(.cs)          Toolbar window, hotkeys, record/play wiring
├── Service/
│   ├── Recorder.cs               Global input hooks -> list of RecordedAction
│   ├── Player.cs                 Replays a list of RecordedAction
│   ├── RecordedAction.cs         The recorded action model (JSON schema)
│   ├── GlobalHotKeyManager.cs    Win32 RegisterHotKey wrapper
│   ├── TreeViewController.cs     Load / save / backup and the other menu actions
│   └── AppSettings.cs            Settings model and the data folder
├── View/
│   ├── FileItemTreeView_UserControl.xaml(.cs)   Right-click context menu
│   └── SettingsWindow.xaml(.cs)                 Shortcuts, folders, auto-stop
├── Themes/
│   └── Ios.xaml                  Colors and control styles shared by all windows
└── Library/
    └── NativeInput.cs            Win32 SendInput wrapper for simulated keys and mouse buttons
```
