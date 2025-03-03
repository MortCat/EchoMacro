using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EchoMacro;

public partial class MainWindow : Window
{
    private Recorder recorder = new Recorder();
    private Player player = new Player();
    public MainWindow()
    {
        InitializeComponent();
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
        int delay = int.TryParse(txtDelay.Text, out var d) ? d : 0;
        bool repeat = chkRepeat.IsChecked ?? false;
        await player.PlayActions(recorder.GetRecordedActions(), delay, repeat);
    }
}