using EchoMacro.Service;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace EchoMacro
{
    public partial class MainWindow : Window
    {
        public Recorder Recorder { get; set; } = new Recorder();
        public Player Player { get; set; } = new Player();
        private TreeViewController? _fileHandler;
        private GlobalHotKeyManager? _hotKeyManager;

        public MainWindow()
        {
            InitializeComponent();
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Process();
        }

        private void Process()
        {
            EnsureWindowOnTop();
            RegisterHotKeys();
            _fileHandler = new TreeViewController(TreeViewMenu, Recorder);
            _fileHandler.LoadFileSuccessfully += () => TreeViewMenu.SaveRecord.IsEnabled = true;
        }
        private void EnsureWindowOnTop()
        {
            this.Topmost = true;
            this.Deactivated += (s, e) => this.Topmost = true;
        }
        private void RegisterHotKeys()
        {
            _hotKeyManager = new GlobalHotKeyManager(this);
            _hotKeyManager.RegisterHotKey(9000, Key.Escape, () => this.Close());
            _hotKeyManager.RegisterHotKey(9001, Key.Space, async () => await TogglePlayback());//Player.StopPlayback());
                                                                                               //this.KeyDown += (sender, e) => {
                                                                                               //    if (e.Key == Key.Escape)
                                                                                               //        this.Close();

            //    if (e.Key == Key.Space)
            //        Player.StopPlayback();
            //};
        }


        private void ToggleRecording(object sender, RoutedEventArgs e)
        {
            if (!Recorder.IsRecording)
            {
                Recorder.StartRecording();
                TreeViewMenu.SaveRecord.IsEnabled = false;
            }
            else
            {
                Recorder.StopRecording();
            }
        }
        private async void BtnPlay_Click(object sender, RoutedEventArgs e) => await TogglePlayback();
        public async Task TogglePlayback()
        {
            if (Player.IsPlaying)
            {
                Player.StopPlayback();
            }
            else
            {
                int delay = int.TryParse(txtDelay.Text, out var sec) ? sec : 0;
                bool repeat = chkRepeat.IsChecked ?? false;
                List<RecordedAction> actions = Recorder.GetRecordedActions();

                if (actions.Count > 0)
                {
                    await Player.PlayActions(actions, delay, repeat);
                }
            }
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

        protected override void OnClosed(EventArgs e)
        {
            _hotKeyManager?.Dispose();
            _fileHandler?.Dispose();
            base.OnClosed(e);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}