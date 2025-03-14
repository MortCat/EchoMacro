using System.Diagnostics;
using Gma.System.MouseKeyHook;
using System.Windows.Forms;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

public class Recorder
{
    public string Name { get; set; }
    private IKeyboardMouseEvents _globalHook;
    private List<RecordedAction> _recordedActions = new List<RecordedAction>();
    private Stopwatch _stopwatch;
    private bool _isRecording;

    public Recorder()
    {
        _recordedActions = new List<RecordedAction>();
        _stopwatch = new Stopwatch();
    }   
    public void StartRecording()
    {
        if (_isRecording) return;
        Init();
    }
    private void Init()
    {
        _recordedActions.Clear();
        _stopwatch.Restart();
        _isRecording = true;
        Name = "Recorded" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

        _globalHook = Hook.GlobalEvents();
        _globalHook.MouseDown += OnMouseDown;
        _globalHook.KeyDown += OnKeyDown;
    }

    public void StopRecording()
    {
        if (!_isRecording) return;

        _globalHook.MouseDown -= OnMouseDown;
        _globalHook.KeyDown -= OnKeyDown;
        _globalHook.Dispose();
        _stopwatch.Stop();

        _isRecording = false;
        if (_recordedActions.Any())
            _recordedActions.RemoveAt(_recordedActions.Count - 1);
    }

    private void OnMouseDown(object sender, MouseEventArgs e)
    {
        if (!_isRecording) return;

        _recordedActions.Add(new RecordedAction
        {
            Type = InputType.MouseClick,
            Timestamp = _stopwatch.Elapsed.TotalMilliseconds,
            X = e.X,
            Y = e.Y,
            IsRightClick = e.Button == MouseButtons.Right
        });
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
       if (!_isRecording) return;

        Keys a = e.KeyCode;
        string keyName = e.KeyCode.ToString();
        _recordedActions.Add(new RecordedAction
        {
            Type = InputType.KeyPress,
            Timestamp = _stopwatch.Elapsed.TotalMilliseconds,
            Key = keyName
        });
    }
    public List<RecordedAction> GetRecordedActions() => new List<RecordedAction>(_recordedActions);
    public void SetRecorder(string recorderName, List<RecordedAction> records)
    {
        Name = recorderName;
        _recordedActions = records;
    }
}
