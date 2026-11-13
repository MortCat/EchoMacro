using EchoMacro.Service;
using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EchoMacro.View
{
    /// <summary>
    /// Edits <see cref="AppSettings.Current"/>. Nothing is changed until Done; Cancel discards everything.
    /// The caller is expected to have the global hotkeys unregistered while this is open, otherwise
    /// pressing a current shortcut would trigger it instead of reaching the shortcut field.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private const double MaxAutoStopSeconds = 60;

        private readonly AppSettings _settings = AppSettings.Current;

        //Working copies, written back on Done.
        private readonly Dictionary<Button, HotKeySetting> _hotKeys = new Dictionary<Button, HotKeySetting>();
        private string? _recordingsFolder;
        private string? _backupFolder;

        private Button? _capturing;

        public SettingsWindow()
        {
            InitializeComponent();

            _hotKeys[btnRecordHotKey] = Copy(_settings.RecordHotKey);
            _hotKeys[btnPlayHotKey] = Copy(_settings.PlayHotKey);
            _hotKeys[btnCloseHotKey] = Copy(_settings.CloseHotKey);
            foreach (Button button in _hotKeys.Keys)
                button.Content = _hotKeys[button].ToString();

            _recordingsFolder = _settings.RecordingsFolder;
            _backupFolder = _settings.BackupFolder;
            ShowFolders();

            swAutoStop.IsChecked = _settings.AutoStopOnMouseMove;
            txtAutoStopSeconds.Text = _settings.AutoStopSeconds.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static HotKeySetting Copy(HotKeySetting source) => new HotKeySetting(source.Modifiers, source.Key);

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        // ---- Shortcuts ----

        private void HotKeyButton_Click(object sender, RoutedEventArgs e)
        {
            EndCapture();
            _capturing = (Button)sender;
            _capturing.Content = "Press shortcut…";
            _capturing.Focus();
            ShowError(null);
        }

        private void HotKeyButton_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!ReferenceEquals(sender, _capturing) || _capturing == null)
                return;

            e.Handled = true;

            //With Alt held, WPF reports the real key in SystemKey.
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key == Key.Escape)
            {
                EndCapture();
                return;
            }
            if (IsModifierKey(key))
                return; //Still waiting for the main key.

            ModifierKeys modifiers = Keyboard.Modifiers;
            bool isFunctionKey = key >= Key.F1 && key <= Key.F24;
            if (modifiers == ModifierKeys.None && !isFunctionKey)
            {
                ShowError("Include Ctrl, Alt, Shift or Win. A single key as a global shortcut would stop working everywhere else.");
                return;
            }

            _hotKeys[_capturing] = new HotKeySetting(modifiers, key);
            EndCapture();
            ShowError(FindDuplicateShortcut());
        }

        private void HotKeyButton_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (ReferenceEquals(sender, _capturing))
                EndCapture();
        }

        private void EndCapture()
        {
            if (_capturing == null)
                return;

            _capturing.Content = _hotKeys[_capturing].ToString();
            _capturing = null;
        }

        private static bool IsModifierKey(Key key) =>
            key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin;

        private string? FindDuplicateShortcut()
        {
            List<HotKeySetting> all = _hotKeys.Values.ToList();
            for (int i = 0; i < all.Count; i++)
            {
                for (int j = i + 1; j < all.Count; j++)
                {
                    if (all[i].SameAs(all[j]))
                        return $"{all[i]} is used for more than one action.";
                }
            }
            return null;
        }

        // ---- Folders ----

        private string RecordingsFolder =>
            string.IsNullOrWhiteSpace(_recordingsFolder) ? new AppSettings().EffectiveRecordingsFolder : _recordingsFolder;
        private string BackupFolder =>
            string.IsNullOrWhiteSpace(_backupFolder) ? new AppSettings().EffectiveBackupFolder : _backupFolder;

        private void ShowFolders()
        {
            txtRecordingsFolder.Text = ShortenPath(RecordingsFolder);
            txtRecordingsFolder.ToolTip = RecordingsFolder;
            txtBackupFolder.Text = ShortenPath(BackupFolder);
            txtBackupFolder.ToolTip = BackupFolder;
        }

        /// <summary>Keeps the end of a long path, which is the part that tells folders apart. The tooltip has all of it.</summary>
        private static string ShortenPath(string path)
        {
            const int maxLength = 34;
            if (path.Length <= maxLength)
                return path;

            string tail = path.Substring(path.Length - maxLength);
            int separator = tail.IndexOf(Path.DirectorySeparatorChar);
            if (separator > 0 && separator < maxLength / 2)
                tail = tail.Substring(separator);

            return "…" + tail;
        }

        private void OpenRecordingsFolder_Click(object sender, RoutedEventArgs e) => OpenFolder(RecordingsFolder);
        private void OpenBackupFolder_Click(object sender, RoutedEventArgs e) => OpenFolder(BackupFolder);

        private void ChangeRecordingsFolder_Click(object sender, RoutedEventArgs e)
        {
            if (PickFolder("Choose the recordings folder", RecordingsFolder) is string folder)
            {
                _recordingsFolder = folder;
                ShowFolders();
            }
        }
        private void ChangeBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            if (PickFolder("Choose the backups folder", BackupFolder) is string folder)
            {
                _backupFolder = folder;
                ShowFolders();
            }
        }

        private void OpenFolder(string folder)
        {
            try
            {
                Directory.CreateDirectory(folder);
                Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowError($"Could not open the folder: {ex.Message}");
            }
        }

        private string? PickFolder(string title, string current)
        {
            var dialog = new OpenFolderDialog { Title = title };
            if (Directory.Exists(current))
                dialog.InitialDirectory = current;

            return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
        }

        // ---- Done / Cancel ----

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            EndCapture();

            if (FindDuplicateShortcut() is string duplicate)
            {
                ShowError(duplicate);
                return;
            }

            bool autoStop = swAutoStop.IsChecked == true;
            double seconds = _settings.AutoStopSeconds;
            if (autoStop)
            {
                string text = txtAutoStopSeconds.Text.Trim().Replace(',', '.');
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds)
                    || seconds <= 0 || seconds > MaxAutoStopSeconds)
                {
                    ShowError($"Enter a time between 0 and {MaxAutoStopSeconds} seconds.");
                    return;
                }
            }

            _settings.RecordHotKey = _hotKeys[btnRecordHotKey];
            _settings.PlayHotKey = _hotKeys[btnPlayHotKey];
            _settings.CloseHotKey = _hotKeys[btnCloseHotKey];
            _settings.RecordingsFolder = _recordingsFolder;
            _settings.BackupFolder = _backupFolder;
            _settings.AutoStopOnMouseMove = autoStop;
            _settings.AutoStopSeconds = seconds;
            _settings.Save();

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void ShowError(string? message)
        {
            txtError.Text = message ?? string.Empty;
            txtError.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
