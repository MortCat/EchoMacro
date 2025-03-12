using System.Diagnostics;
using Gma.System.MouseKeyHook;
using System.Windows.Forms;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using System.Xml.Linq;

public class Recorder
{
    public string Name { get; set; }
    private IKeyboardMouseEvents globalHook;
    private List<RecordedAction> recordedActions = new List<RecordedAction>();
    private Stopwatch stopwatch;
    private bool isRecording;

    public Recorder()
    {
        recordedActions = new List<RecordedAction>();
        stopwatch = new Stopwatch();
    }   
    public void StartRecording()
    {
        if (isRecording) return;
        Init();
    }
    private void Init()
    {
        recordedActions.Clear();
        stopwatch.Restart();
        isRecording = true;
        Name = "Recorded" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

        globalHook = Hook.GlobalEvents();
        globalHook.MouseDown += OnMouseDown;
        globalHook.KeyDown += OnKeyDown;
    }

    public void StopRecording()
    {
        if (!isRecording) return;

        globalHook.MouseDown -= OnMouseDown;
        globalHook.KeyDown -= OnKeyDown;
        globalHook.Dispose();
        stopwatch.Stop();

        isRecording = false;
    }

    private void OnMouseDown(object sender, MouseEventArgs e)
    {
        if (!isRecording) return;

        recordedActions.Add(new RecordedAction
        {
            Type = InputType.MouseClick,
            Timestamp = stopwatch.Elapsed.TotalMilliseconds,
            X = e.X,
            Y = e.Y,
            IsRightClick = e.Button == MouseButtons.Right
        });
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
       if (!isRecording) return;

        Keys a = e.KeyCode;
        string keyName = e.KeyCode.ToString();
        recordedActions.Add(new RecordedAction
        {
            Type = InputType.KeyPress,
            Timestamp = stopwatch.Elapsed.TotalMilliseconds,
            Key = keyName
        });
    }
    public List<RecordedAction> GetRecordedActions() => new List<RecordedAction>(recordedActions);
    public void SetRecorder(string recorderName, List<RecordedAction> records)
    {
        Name = recorderName;
        recordedActions = records;
    }
}
