using EchoMacro.Service;
using System.Windows;
using System.Windows.Input;

namespace EchoMacro;

public partial class MainWindow : Window
{
    private Recorder _recorder = new Recorder();
    private Player _player = new Player();
    private TreeViewController? _fileHandler;

    public MainWindow()
    {
        InitializeComponent();
        Process();
    }
    private void Process()
    {
        EnsureWindowOnTop();

        this.KeyDown += (sender, e) => {
            if (e.Key == Key.Escape)
                this.Close();
        };
        _fileHandler = new TreeViewController(TreeViewMenu, _recorder);
        _fileHandler.LoadFileSuccessfully += () => TreeViewMenu.SaveRecord.IsEnabled = true;
    }
    private void EnsureWindowOnTop()
    {
        this.Topmost = true;
        this.Deactivated += (s, e) => this.Topmost = true;
    }


    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        _recorder.StartRecording();
        TreeViewMenu.SaveRecord.IsEnabled = false;
        btnStart.IsEnabled = false;
        btnStop.IsEnabled = true;
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        _recorder.StopRecording();
        btnStart.IsEnabled = true;
        btnStop.IsEnabled = false;
    }

    private async void BtnPlay_Click(object sender, RoutedEventArgs e)
    {
        int delay = int.TryParse(txtDelay.Text, out var sec) ? sec : 0;
        bool repeat = chkRepeat.IsChecked ?? false;
        await _player.PlayActions(_recorder.GetRecordedActions(), delay, repeat);
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
}