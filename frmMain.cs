using System.Security.Policy;

namespace WinNTC1750;

sealed partial class frmMain : Form
{
    private string moduleName = "frmMain.cs";

    private Board board = new Board();
    private int _speed = 0;
    private int _incline = 0;
    private int _distance = 0;
    private Boolean _useMetrics = true;
    private readonly Thread _worker;
    private bool _quit = false;
    
    public frmMain()
    {
        InitializeComponent();
        _worker = new Thread(WorkerThreadProc) { IsBackground = true };
    }

    private void frmMain_Load(object sender, EventArgs e)
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "frmMain_Load", "Starting worker thread...");
        _worker.Start();
    }

    private void frmMain_Leave(object sender, EventArgs e)
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "frmMain_Leave", "Unloading frmMain...");
        if(board.Status() == Board.STATUS.CONNECTED && (board.Mode() == Board.MODE.RUNNING || board.Mode() == Board.MODE.WARM_UP || board.Mode() == Board.MODE.COOL_DOWN))
        {
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "frmMain_Leave", "Belt was running. Requesting stop...");
            
        }
        _quit = true;
    }

    public void processUI(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try
        {
            BeginInvoke(action);
        }
        catch(Exception e)
        {
            Helper.Log(Helper.LEVELS.ERROR, moduleName, "ProcessUI", "Failed to process UI: " + e.Message.ToString());  
        }
    }
    
    private void btnStart_Click(object sender, EventArgs e)
    {
        StartBelt(true);
    }

    private void btnStop_Click(object sender, EventArgs e)
    {
        StopBelt(true);
    }

    void UpdateScreen()
    {
        //Helper.Log(Helper.LEVELS.DEBUG, moduleName, "UpdateScreen", "Updating Screen...");

        if (board.Status() == Board.STATUS.UNKNOWN)
        {
            lblMode.Text = "Mode: ";
            lblSpeed.Text = "Speed:";
            lblIncline.Text = "Incline:";
            lblDistance.Text = "Distance:";
            lblStatus.Text = "Board not Found. Please connect it."; 
            btnStart.BackColor = Color.DarkGray;
            btnStop.BackColor = Color.DarkGray;
 
        }
        else
        {
            lblMode.Text = $"Mode: {board.Mode().ToString()}.";
            lblSpeed.Text = $"Speed: {_speed + $" {(_useMetrics ? "km/h" : "miles/h")}"}.";
            lblIncline.Text = $"Incline: {_incline + " degrees"}.";
            lblDistance.Text = $"Distance: {(board.Status() == Board.STATUS.UNKNOWN ? "?" : _distance + $" {(_useMetrics ? "meters" : "miles")}")} .";
            lblStatus.Text = (board.Status() == Board.STATUS.ERROR ? board.lastErrorMsg() : $"{board.Status().ToString()}."); 
            btnStart.BackColor = Color.LightGreen;
            btnStop.BackColor = Color.Firebrick;
        }

        btnStart.Enabled = (board.Status() == Board.STATUS.CONNECTED && (board.Mode() == Board.MODE.IDLE || board.Mode() == Board.MODE.PAUSE));
        btnStop.Enabled =  (board.Status() == Board.STATUS.CONNECTED && (board.Mode() == Board.MODE.RUNNING || board.Mode() == Board.MODE.WARM_UP || board.Mode() == Board.MODE.COOL_DOWN));
        
    }

    void StartBelt(Boolean warmUp = false)
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "StartBelt", "Stopping belt. " + (warmUp ? " Warming up first..." : "..."));     
        //TODO
    }

    void StopBelt(Boolean coolDown = false) 
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "StopBelt", "Stopping belt. " + (coolDown ? " Cooling down first..." : "..."));
        //TODO
    }

    void WorkerThreadProc()
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "WorkerThreadProc", "Entering worker thread loop..." );
        while (!_quit)
        {
            try
            {
                switch (board.Status())
                {
                    case Board.STATUS.UNKNOWN:
                    case Board.STATUS.DISCONNECTED:
                        board.Open();
                        Thread.Sleep(1000); // Waits 1sec only to allow enough time for the USB device to be plugged in
                        break;
                    
                    case Board.STATUS.CONNECTING: 
                        break; 
                    
                    case Board.STATUS.HANDSHAKING:
                        board.Handshake();
                        break;
                    
                    case Board.STATUS.CONNECTED: 
                        switch (board.Mode())
                        {
                            case Board.MODE.UNKNOWN:
                                break;
                            case Board.MODE.IDLE:
                                break;
                            case Board.MODE.RUNNING:
                                break;
                            case Board.MODE.PAUSE:
                                break;
                            case Board.MODE.RESULTS:
                                break;
                            case Board.MODE.DMK:
                                break;
                            case Board.MODE.WARM_UP:
                                break;
                            case Board.MODE.COOL_DOWN:
                                break;
                            case Board.MODE.RESUME:
                                break;
                        }
                        break; 
                    
                    case Board.STATUS.ERROR:
                        break;
                };
        
                processUI(() =>
                {
                    UpdateScreen();
                });
                
            }
            catch (Exception e)
            {
                Helper.Log(Helper.LEVELS.ERROR, moduleName, "WorkerThreadProc", "Failed: " + e.Message.ToString());
            }
            Thread.Sleep(1);
        }
        
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "WorkerThreadProc", "Exited worker thread loop." );
    }
    
}
