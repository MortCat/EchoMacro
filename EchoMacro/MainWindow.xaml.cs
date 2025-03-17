using EchoMacro.Service;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace EchoMacro;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private bool _isRecording;
    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            _isRecording = value;
            OnPropertyChanged();
        }
    }
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
        SetShortcutKeys();
        _fileHandler = new TreeViewController(TreeViewMenu, _recorder);
        _fileHandler.LoadFileSuccessfully += () => TreeViewMenu.SaveRecord.IsEnabled = true;
    }
    private void EnsureWindowOnTop()
    {
        this.Topmost = true;
        this.Deactivated += (s, e) => this.Topmost = true;
    }
    private void SetShortcutKeys()
    {
        this.KeyDown += (sender, e) => {
            if (e.Key == Key.Escape)
                this.Close();

            if (e.Key == Key.Space)
                _player.StopPlayback();
        };
    }


    private void ToggleRecording(object sender, RoutedEventArgs e)
    {
        if (!IsRecording)
        {
            _recorder.StartRecording();
            TreeViewMenu.SaveRecord.IsEnabled = false;
        }
        else
        {
            _recorder.StopRecording();
        }
        IsRecording = !IsRecording;
    }
    private async void BtnPlay_Click(object sender, RoutedEventArgs e)
    {
        int delay = int.TryParse(txtDelay.Text, out var sec) ? sec : 0;
        bool repeat = chkRepeat.IsChecked ?? false;
        List<RecordedAction> actions = _recorder.GetRecordedActions();
        if (actions.Count == 0)
            return;

        await _player.PlayActions(actions, delay, repeat);
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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}