using EchoMacro.View;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace EchoMacro.Service
{
    public class TreeViewController
    {
        public event Action? LoadFileSuccessfully;
        private readonly Recorder _recorder;

        public TreeViewController(FileTreeView_UserControl itemControl, Recorder recorder)
        {
            _recorder = recorder;
            itemControl.OnLoadRecord += HandleLoadRecord;
            itemControl.OnSaveAsRecord += HandleSaveAsRecord;
            itemControl.OnSaveRecord += HandleSaveRecord;
            itemControl.OnMinimizeApp += HandleMinimizeApp;
            itemControl.OnCloseApp += HandleCloseApp;
        }
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
    }
}
