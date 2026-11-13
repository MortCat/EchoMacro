using EchoMacro.View;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace EchoMacro.Service
{
    public class TreeViewController
    {
        /// <summary>Raised when the record becomes tied to a file (loaded from it or saved to it), so "Save" has a target.</summary>
        public event Action? CurrentFileChanged;
        private readonly Recorder _recorder;
        private string? _currentFilePath;

        private const int MaxBackups = 10;
        private static string BackupFolder => AppSettings.Current.EffectiveBackupFolder;

        public TreeViewController(FileTreeView_UserControl itemControl, Recorder recorder)
        {
            _recorder = recorder;
            itemControl.OnLoadRecord += HandleLoadRecord;
            itemControl.OnSaveAsRecord += HandleSaveAsRecord;
            itemControl.OnSaveRecord += HandleSaveRecord;
            itemControl.OnMinimizeApp += HandleMinimizeApp;
            itemControl.OnCloseApp += HandleCloseApp;
        }

        private void HandleLoadRecord()
        {
            if (_recorder.IsRecording)
            {
                MessageBox.Show("Stop recording before loading a record.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Open JSON File",
                InitialDirectory = GetRecordingsFolder(),
                Filter = "JSON Files (*.json)|*.json",
                DefaultExt = "json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                try
                {
                    string jsonString = File.ReadAllText(filePath);
                    List<RecordedAction>? loadedActions = JsonSerializer.Deserialize<List<RecordedAction>>(jsonString);

                    if (loadedActions != null && !TryValidate(loadedActions, out string problem))
                    {
                        MessageBox.Show($"The file is not a valid record:\n{problem}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (loadedActions != null)
                    {
                        //Loading replaces the current recording; keep a copy if it was never saved.
                        BackupUnsavedRecording();
                        _recorder.SetRecorder(openFileDialog.SafeFileName, loadedActions);
                        _currentFilePath = filePath;
                        CurrentFileChanged?.Invoke();
                    }
                    else
                    {
                        MessageBox.Show("Failed to parse the file.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while loading the file:\n{ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void HandleSaveAsRecord()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Save As",
                InitialDirectory = GetRecordingsFolder(),
                Filter = "JSON Files (*.json)|*.json",
                DefaultExt = "json",
                FileName = "RecordedActions.json"
            };

            if (saveFileDialog.ShowDialog() == true && TryWriteRecord(saveFileDialog.FileName))
            {
                _currentFilePath = saveFileDialog.FileName;
                CurrentFileChanged?.Invoke();
            }
        }
        private void HandleSaveRecord()
        {
            if (_recorder.GetRecordedActions().Count == 0)
            {
                MessageBox.Show("No recorded actions to save.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //Without a known file there is nothing to overwrite, so ask where to save.
            if (_currentFilePath == null)
            {
                HandleSaveAsRecord();
                return;
            }
            TryWriteRecord(_currentFilePath);
        }
        /// <summary>The folder the file dialogs start in, created on first use. Empty lets Windows decide.</summary>
        private static string GetRecordingsFolder()
        {
            try
            {
                string folder = AppSettings.Current.EffectiveRecordingsFolder;
                Directory.CreateDirectory(folder);
                return folder;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        private bool TryWriteRecord(string filePath)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(_recorder.GetRecordedActions(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, jsonString);
                _recorder.MarkSaved();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving the file:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        /// <summary>
        /// Silently writes a recording that was never saved to the backup folder, so replacing it or closing the
        /// app cannot lose it. Only the most recent backups are kept. Never fails and never asks.
        /// </summary>
        public void BackupUnsavedRecording()
        {
            if (!_recorder.HasUnsavedChanges)
                return;

            try
            {
                Directory.CreateDirectory(BackupFolder);
                string filePath = Path.Combine(BackupFolder, _recorder.Name + ".json");
                File.WriteAllText(filePath, JsonSerializer.Serialize(_recorder.GetRecordedActions(), new JsonSerializerOptions { WriteIndented = true }));
                _recorder.MarkSaved();

                var oldBackups = new DirectoryInfo(BackupFolder).GetFiles("*.json")
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .Skip(MaxBackups);
                foreach (FileInfo file in oldBackups)
                    file.Delete();
            }
            catch (Exception)
            {
                //A backup is a safety net; failing to write one must not block recording or closing.
            }
        }

        private static bool TryValidate(List<RecordedAction> actions, out string problem)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                RecordedAction? action = actions[i];
                if (action == null)
                {
                    problem = $"Action #{i + 1} is empty.";
                    return false;
                }
                if (!Enum.IsDefined(action.Type))
                {
                    problem = $"Action #{i + 1} has an unknown type.";
                    return false;
                }
                if (!double.IsFinite(action.Timestamp) || action.Timestamp < 0)
                {
                    problem = $"Action #{i + 1} has an invalid timestamp.";
                    return false;
                }
                bool isKeyAction = action.Type is InputType.KeyPress or InputType.KeyDown or InputType.KeyUp;
                bool hasKeyCode = action.KeyCode > 0 && action.KeyCode < 0xFF;
                if (isKeyAction && !hasKeyCode && string.IsNullOrEmpty(action.Key))
                {
                    problem = $"Action #{i + 1} is a key action without a key.";
                    return false;
                }
                if (!Enum.IsDefined(action.Button))
                {
                    problem = $"Action #{i + 1} has an unknown mouse button.";
                    return false;
                }
                action.Key ??= string.Empty;
            }
            problem = string.Empty;
            return true;
        }
        private void HandleMinimizeApp() => (Application.Current.MainWindow as MainWindow)?.MinimizeToTray();
        private void HandleCloseApp() => Application.Current.MainWindow.Close();
    }
}
