using EchoMacro.Service;
using EchoMacro.View;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace EchoMacro
{
    public partial class MainWindow : Window
    {
        private const int HotKeyIdClose = 9000;
        private const int HotKeyIdTogglePlayback = 9001;
        private const int HotKeyIdToggleRecording = 9002;

        private readonly AppSettings _settings = AppSettings.Current;
        private TreeViewController? _fileHandler;
        private GlobalHotKeyManager? _hotKeyManager;

        /// <summary>Width of the slide-out delay panel when open; matches its content in MainWindow.xaml.</summary>
        private const double DelayPanelWidth = 140;
        private static readonly TimeSpan DelayPanelSlideTime = TimeSpan.FromMilliseconds(220);
        //A short grace period, so brushing past the edge of the toolbar does not snap the panel shut.
        private readonly DispatcherTimer _collapseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        private bool _delayPanelOpen;

        //Shown in the notification area while the toolbar is hidden there.
        private System.Windows.Forms.NotifyIcon? _trayIcon;

        public Recorder Recorder { get; } = new Recorder();
        public Player Player { get; } = new Player();


        public MainWindow()
        {
            InitializeComponent();
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            EnsureWindowOnTop();
            RegisterHotKeys();
            InitializeFileHandler();
            RestoreSettings();
            UpdatePlayButton();

            _collapseTimer.Tick += (s, e) =>
            {
                _collapseTimer.Stop();
                if (!Toolbar.IsMouseOver && !txtDelay.IsKeyboardFocusWithin)
                    SlideDelayPanel(open: false);
            };
        }

        // ---- Notification area ----
        //"Minimize" hides the toolbar into the notification area. The window still exists, so the global
        //hotkeys keep working; the tray icon's tooltip tells what the app is doing meanwhile.

        public void MinimizeToTray()
        {
            if (_trayIcon == null)
            {
                var menu = new System.Windows.Forms.ContextMenuStrip();
                menu.Items.Add("Show EchoMacro", null, (s, e) => RestoreFromTray());
                menu.Items.Add("Close", null, (s, e) => Close());

                _trayIcon = new System.Windows.Forms.NotifyIcon
                {
                    Icon = LoadApplicationIcon(),
                    ContextMenuStrip = menu
                };
                _trayIcon.MouseClick += (s, e) =>
                {
                    if (e.Button == System.Windows.Forms.MouseButtons.Left)
                        RestoreFromTray();
                };

                Recorder.PropertyChanged += (s, e) => UpdateTrayText();
                Player.PropertyChanged += (s, e) => Dispatcher.Invoke(UpdateTrayText);
            }

            UpdateTrayText();
            _trayIcon.Visible = true;
            Hide();
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            if (_trayIcon != null)
                _trayIcon.Visible = false;
        }

        private void UpdateTrayText()
        {
            if (_trayIcon == null)
                return;

            string state = Recorder.IsRecording ? "Recording" : Player.IsPlaying ? "Playing" : "Ready";
            _trayIcon.Text = $"EchoMacro - {state}";
        }

        private static System.Drawing.Icon LoadApplicationIcon()
        {
            try
            {
                if (Environment.ProcessPath is string path
                    && System.Drawing.Icon.ExtractAssociatedIcon(path) is System.Drawing.Icon icon)
                    return icon;
            }
            catch (Exception)
            {
                //Fall back to a stock icon; a tray icon without an image would be invisible.
            }
            return System.Drawing.SystemIcons.Application;
        }

        /// <summary>Play needs something to play, and is off limits while recording.</summary>
        private void UpdatePlayButton()
        {
            bool canPlay = Recorder.HasActions && !Recorder.IsRecording;
            btnPlay.IsEnabled = canPlay || Player.IsPlaying;
            btnPlay.ToolTip = canPlay || Player.IsPlaying
                ? $"Play / Stop ({_settings.PlayHotKey})"
                : "Record or load a recording first";
        }

        // ---- Slide-out delay panel ----
        //Opens when the pointer reaches the repeat icon; stays open while the pointer is anywhere on the
        //toolbar or the delay field has the keyboard, so it does not close under the user's hands.

        private void ChkRepeat_MouseEnter(object sender, MouseEventArgs e) => SlideDelayPanel(open: true);
        private void Toolbar_MouseEnter(object sender, MouseEventArgs e) => _collapseTimer.Stop();
        private void Toolbar_MouseLeave(object sender, MouseEventArgs e) => _collapseTimer.Start();
        private void TxtDelay_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!Toolbar.IsMouseOver)
                _collapseTimer.Start();
        }

        private void SlideDelayPanel(bool open)
        {
            _collapseTimer.Stop();
            if (_delayPanelOpen == open)
                return;
            _delayPanelOpen = open;

            var animation = new DoubleAnimation
            {
                To = open ? DelayPanelWidth : 0,
                Duration = DelayPanelSlideTime,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            DelayPanel.BeginAnimation(WidthProperty, animation);
        }
        private void EnsureWindowOnTop()
        {
            this.Topmost = true;
            this.Deactivated += (s, e) => this.Topmost = true;
        }

        /// <summary>(Re)registers the global hotkeys from the settings and tells the user about any that are taken.</summary>
        private void RegisterHotKeys()
        {
            _hotKeyManager ??= new GlobalHotKeyManager(this);
            _hotKeyManager.UnregisterAllHotKeys();

            var unavailable = new List<string>();
            void Register(int id, HotKeySetting hotKey, Action action)
            {
                if (!_hotKeyManager.RegisterHotKey(id, hotKey.Modifiers, hotKey.Key, action))
                    unavailable.Add(hotKey.ToString());
            }

            Register(HotKeyIdToggleRecording, _settings.RecordHotKey, ToggleRecording);
            Register(HotKeyIdTogglePlayback, _settings.PlayHotKey, async () => await TogglePlayback());
            Register(HotKeyIdClose, _settings.CloseHotKey, () => this.Close());

            btnStart.ToolTip = $"Record / Stop ({_settings.RecordHotKey})";
            UpdatePlayButton();

            if (unavailable.Count > 0)
            {
                MessageBox.Show(this,
                    $"These shortcuts are already used by another application and will not work:\n\n{string.Join("\n", unavailable)}\n\nChoose different ones in Settings (right-click the toolbar).",
                    "EchoMacro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void InitializeFileHandler()
        {
            _fileHandler = new TreeViewController(TreeViewMenu, Recorder);
            _fileHandler.CurrentFileChanged += () =>
            {
                TreeViewMenu.SaveRecord.IsEnabled = true;
                UpdatePlayButton();
            };
            TreeViewMenu.OnOpenSettings += OpenSettings;
        }

        private void OpenSettings()
        {
            if (Recorder.IsRecording || Player.IsPlaying)
                return;

            //While the hotkeys are registered Windows swallows them, so the shortcut fields could never see them.
            _hotKeyManager?.UnregisterAllHotKeys();
            try
            {
                new SettingsWindow { Owner = this }.ShowDialog();
            }
            finally
            {
                RegisterHotKeys();
            }
        }

        private void RestoreSettings()
        {
            txtDelay.Text = Math.Max(0, _settings.Delay).ToString();
            chkRepeat.IsChecked = _settings.Repeat;

            //Only restore a position that is still on a connected screen.
            if (_settings.WindowLeft is double left && _settings.WindowTop is double top
                && left >= SystemParameters.VirtualScreenLeft
                && top >= SystemParameters.VirtualScreenTop
                && left + ActualWidth <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth
                && top + ActualHeight <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
            {
                Left = left;
                Top = top;
            }
        }
        private void SaveSettings()
        {
            //A minimized window reports an off-screen position; its restore bounds hold the real one.
            Rect bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, ActualWidth, ActualHeight) : RestoreBounds;

            _settings.Delay = int.TryParse(txtDelay.Text, out var ms) ? Math.Max(0, ms) : 0;
            _settings.Repeat = chkRepeat.IsChecked ?? false;
            if (!bounds.IsEmpty)
            {
                _settings.WindowLeft = bounds.Left;
                _settings.WindowTop = bounds.Top;
            }
            _settings.Save();
        }


        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
        private void BtnStart_Click(object sender, RoutedEventArgs e) => ToggleRecording();
        public void ToggleRecording()
        {
            if (!Recorder.IsRecording)
            {
                //Recording during playback would capture the macro's own simulated input.
                if (Player.IsPlaying)
                    return;

                //A new recording replaces the current one; keep a copy if it was never saved.
                _fileHandler?.BackupUnsavedRecording();

                Recorder.StartRecording();
                TreeViewMenu.SaveRecord.IsEnabled = false;
            }
            else
            {
                Recorder.StopRecording();
            }
            UpdatePlayButton();
        }
        private async void BtnPlay_Click(object sender, RoutedEventArgs e) => await TogglePlayback();
        public async Task TogglePlayback()
        {
            if (Player.IsPlaying)
            {
                Player.StopPlayback();
                return;
            }

            //The hotkey reaches this even while the Play button is disabled.
            if (Recorder.IsRecording)
                return;

            int delay = int.TryParse(txtDelay.Text, out var ms) ? Math.Max(0, ms) : 0;
            bool repeat = chkRepeat.IsChecked ?? false;
            List<RecordedAction> actions = Recorder.GetRecordedActions();

            if (actions.Count == 0)
                return;

            Player.AutoStopSeconds = _settings.AutoStopOnMouseMove ? _settings.AutoStopSeconds : 0;

            btnStart.IsEnabled = false;
            try
            {
                await Player.PlayActions(actions, delay, repeat);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Playback failed:\n{ex.Message}", "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnStart.IsEnabled = true;
            }
        }

        private void Border_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(this);
            TreeViewMenu.Show(position);
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (e.Cancel)
                return;

            Player.StopPlayback();
            Recorder.StopRecording();
            _fileHandler?.BackupUnsavedRecording();
            SaveSettings();
        }
        protected override void OnClosed(EventArgs e)
        {
            _hotKeyManager?.Dispose();
            if (_trayIcon != null)
            {
                //Without this the icon lingers in the notification area until the pointer passes over it.
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            base.OnClosed(e);
        }
    }
}
