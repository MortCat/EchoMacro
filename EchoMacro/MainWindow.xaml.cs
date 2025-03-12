using EchoMacro.Service;
using EchoMacro.View;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.Text.Json;
using System.IO;

namespace EchoMacro;

public partial class MainWindow : Window
{
    private Recorder recorder = new Recorder();
    private Player player = new Player();

    public MainWindow()
    {
        InitializeComponent();
        Process();
    }
    private void Process()
    {
        this.KeyDown += (sender, e) => {
            if (e.Key == Key.Escape)
                this.Close();
        };

        TreeViewMenu.OnLoadRecord += HandleLoadRecord;
        TreeViewMenu.OnSaveAsRecord += HandleSaveAsRecord;
        TreeViewMenu.OnSaveRecord += HandleSaveRecord;
        TreeViewMenu.OnCloseApp += HandleCloseApp;
    }



    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        recorder.StartRecording();
        TreeViewMenu.SaveRecord.IsEnabled = false;
        btnStart.IsEnabled = false;
        btnStop.IsEnabled = true;
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        recorder.StopRecording();
        btnStart.IsEnabled = true;
        btnStop.IsEnabled = false;
    }

    private async void BtnPlay_Click(object sender, RoutedEventArgs e)
    {
        int delay = int.TryParse(txtDelay.Text, out var sec) ? sec : 0;
        bool repeat = chkRepeat.IsChecked ?? false;
        await player.PlayActions(recorder.GetRecordedActions(), delay, repeat);
    }
    private void MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }
    private void Border_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point position = e.GetPosition(this);
        TreeViewMenu.Show(position);
    }


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
                    recorder.SetRecorder(openFileDialog.SafeFileName, loadedActions);
                    MessageBox.Show("File loaded successfully!", "Load Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to parse the file.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                TreeViewMenu.SaveRecord.IsEnabled = true;
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
            string jsonString = JsonSerializer.Serialize(recorder.GetRecordedActions(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, jsonString);
            MessageBox.Show($"File has been successfully saved to {filePath}", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    private void HandleSaveRecord()
    {
        if (recorder.GetRecordedActions().Count == 0)
        {
            MessageBox.Show("No recorded actions to save.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        string jsonString = JsonSerializer.Serialize(recorder.GetRecordedActions(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(recorder.Name, jsonString);
        MessageBox.Show($"File has been successfully saved to {recorder.Name}.json", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void HandleCloseApp() => this.Close();
}