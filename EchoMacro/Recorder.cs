using System.Diagnostics;
using Gma.System.MouseKeyHook;
using System.Windows.Forms;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

public class Recorder
{
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

        recordedActions.Clear();
        stopwatch.Restart();
        isRecording = true;

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

        string keyName = e.KeyCode.ToString();

        if (e.Control) 
            keyName = "Ctrl+" + keyName;
        if (e.Shift) 
            keyName = "Shift+" + keyName;
        if (e.Alt) 
            keyName = "Alt+" + keyName;

        recordedActions.Add(new RecordedAction
        {
            Type = InputType.KeyPress,
            Timestamp = stopwatch.Elapsed.TotalMilliseconds,
            Key = keyName
        });
    }


    public List<RecordedAction> GetRecordedActions() => new List<RecordedAction>(recordedActions);
}
