using EchoMacro.Service;
using EchoMacro.View;
using System.Windows;
using System.Windows.Input;

namespace EchoMacro;

public partial class MainWindow : Window
{
    private Recorder recorder = new Recorder();
    private Player player = new Player();
    private FileTreeView_UserControl treeView = new FileTreeView_UserControl();
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

        treeView.OnLoadRecord += HandleLoadRecord;
        treeView.OnSaveRecord += HandleSaveRecord;
        treeView.OnCloseApp += HandleCloseApp;
    }



    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        recorder.StartRecording();
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        recorder.StopRecording();
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


    private void HandleLoadRecord(RecordedAction recorder)
    {

    }

    private void HandleSaveRecord(RecordedAction recorder)
    {
        if (FileManager.SaveAsRecord(recorder))
        {

        }
    }

    private void HandleCloseApp()
    {
        Application.Current.Shutdown();
    }
}