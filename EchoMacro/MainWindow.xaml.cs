using System.Windows;
using System.Windows.Input;

namespace EchoMacro;

public partial class MainWindow : Window
{
    private Recorder recorder = new Recorder();
    private Player player = new Player();
    public MainWindow()
    {
        InitializeComponent();
        this.KeyDown += (sender, e) => {
            if (e.Key == Key.Escape) 
                this.Close();
        };
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
}