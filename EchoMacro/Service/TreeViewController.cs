using EchoMacro.View;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Input;

namespace EchoMacro.Service
{
    public class TreeViewController
    {
        public event Action? LoadFileSuccessfully;
        private readonly Recorder _recorder;
        private GlobalHotKeyManager? _hotKeyManager;

        public TreeViewController(FileTreeView_UserControl itemControl, Recorder recorder)
        {
            _recorder = recorder;
            InitializeHotKeys();
            itemControl.OnLoadRecord += HandleLoadRecord;
            itemControl.OnSaveAsRecord += HandleSaveAsRecord;
            itemControl.OnSaveRecord += HandleSaveRecord;
            itemControl.OnMinimizeApp += HandleMinimizeApp;
            itemControl.OnCloseApp += HandleCloseApp;
        }
        private void InitializeHotKeys()
        {
            if (Application.Current.MainWindow != null)
            {
                SetupHotKeys();
            }
            else
            {
                Application.Current.Activated += OnApplicationActivated;
            }
            Application.Current.Dispatcher.InvokeAsync(SetupHotKeys);
        }
        private void SetupHotKeys()
        {
            if (Application.Current.MainWindow is Window mainWindow)
            {
                _hotKeyManager = new GlobalHotKeyManager(mainWindow);
                RegisterHotKeys();
            }
        }
        private void OnApplicationActivated(object? sender, EventArgs e)
        {
            if (Application.Current.MainWindow != null)
            {
                Application.Current.Activated -= OnApplicationActivated;
                SetupHotKeys();
            }
        }
        private void RegisterHotKeys() => _hotKeyManager?.RegisterHotKey(9002, Key.Escape, HandleCloseApp);
        private void NotifyFileLoadSuccess() => LoadFileSuccessfully?.Invoke();

        private void HandleLoadRecord()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Open JSON File",
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

                    if (loadedActions != null)
                    {
                        _recorder.SetRecorder(openFileDialog.SafeFileName, loadedActions);
                        NotifyFileLoadSuccess();
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
                Filter = "JSON Files (*.json)|*.json",
                DefaultExt = "json",
                FileName = "RecordedActions.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                string jsonString = JsonSerializer.Serialize(_recorder.GetRecordedActions(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, jsonString);
            }
        }
        private void HandleSaveRecord()
        {
            if (_recorder.GetRecordedActions().Count == 0)
            {
                MessageBox.Show("No recorded actions to save.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string jsonString = JsonSerializer.Serialize(_recorder.GetRecordedActions(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_recorder.Name, jsonString);
        }
        private void HandleMinimizeApp() => Application.Current.MainWindow.WindowState = WindowState.Minimized;
        private void HandleCloseApp() => Application.Current.Shutdown();

        public void Dispose() => _hotKeyManager?.Dispose();
    }
}
