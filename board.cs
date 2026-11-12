using HidSharp;

//  The tablet is detected as            VID: 0E8D (Mediatek) PID:0003 (Mediatek USB Port) 15 for seconds. This is the BROM mode
//  The tablet is sometimes detected as  VID: 0E8D (Mediatek) PID:2000 (Mediatek Preloader USB VCOM). This is a MT6583 preloader mode
//  The tablet is sometimes detected as  VID: 0E8D (Mediatek) PID:2001 (Mediatek DA USB VCOM) 

sealed class Board : IDisposable
{
    private string moduleName = "board.cs";

    private const int VID = 0x213C; //ICON Health & Fitness (Rebranded as iFit Health & Fitness)
    private const int PID = 0x0002; //ICON Generic HID (Good Wolf Universal ZH0058)
    //private const int REPORT_SIZE = 64; //Jornal size for this device

    private const int READ_TIMEOUT = 500; //Timeout on Read operations (Default 500ms)
    private const int WRITE_TIMEOUT = 500; //Timeout on Write operations (Default 500ms)
    private int  MAX_READING_RETRIES = 6;
    
    private HidDevice _device = null!;
    private HidStream _hidStream = null!;
    private STATUS _status = STATUS.UNKNOWN;
    private MODE _mode = MODE.UNKNOWN;
    private int _outLen = 0; //Size (informed by the HID) of the outcoming report
    private int _inLen = 0; //Size (reported by the HID) of the incoming report
    private Boolean _keepTryingSiently = false;
    private string _lastErrorStatus = "";
    
    private byte[] _deviceInfo = new byte[0];
    private byte[] _supportedDevices = new byte[0];
    private byte[] _suportedComands = new byte[0];
    private byte[] _systemInfo = new byte[0];
    private byte[] _versionInfo = new byte[0];
    private byte[] _securityInfo  = new byte[0];

    private int _softwareVersion = 0;
    private uint _serialNumber = 0;
    private uint _model = 0;
    private uint _partNumber = 0;
    private int _masterLibraryVersion = 0;
    
    
    public enum COMMAND : byte
    {
        NONE                    =   0,      // NOT USED
        PORTAL_DEV_LISTEN	    =   1,      // NOT USED
        READ_WRITE              =   2,      // Basically everything is performed using this one.
        TEST                    =   3,      // NOT USED
        CONNECT                 =   4,      // NOT USED
        DISCONNECT              =   5,      // NOT USED
        CALIBRATE               =   6,      // NOT USED
        UPDATE                  =   9,      // NOT USED. Firmware Operation, ⚠️ DO NOT USE IT!!!!!!!!!!!
        ENTER_BOOT_LOADER       =  56,      // NOT USED. Firmware Operation, ⚠️ DO NOT USE IT!!!!!!!!!!!
        SET_TESTING_KEY         = 112,      // NOT USED
        SET_TESDTING_TACH       = 113,      // NOT USED
        SUPPORTED_DEVICES       = 128,      // Lists board's subsystems
        DEVICE_INFO             = 129,      // Lists board's capabilities bitmap
        SYSTEM_INFO             = 130,      // Lists board's parameters
        TASK_INFO               = 131,      // NOT USED
        VERSION_INFO            = 132,      // NOT USED
        SUPPORTED_COMMANDS      = 136,      // Lists board's supported commands
        READ_CONFIG             = 137,      // NOT USED.
        VERIFY_SECURITY         = 144,      // 32 Byte challenge. Board replies 08
        PROTOCOL_DATA           = 145,      // NOT USED
        SPEED_GRADE_LIMIT       = 146,      // NOT USED
        SERIAL_NUMBER           = 149,      // ASCII Brainboard serial number
        RAW                     = 255       // NOT USED
    }

    public enum DEVICE : byte
    {
        NONE	           =  0,		
        MULTIPLE_DEVICES   =  1,		
        MAIN	           =  2,		
        PORTAL	           =  3,		
        TREADMILL	       =  4,		
        INCLINE_TRAINER	   =  5,		
        ELLIPTICAL	       =  6,		
        ROWER	           = 20,		
        HEART_RATE_MONITOR = 55,	
        AUDIO	           = 64,
        SPEED	           = 65,
        GRADE	           = 66,
        WATTS	           = 67,
        PULSE	           = 70,
        KEY_PRESS		   = 71,
        FAN	               = 74,
        SAFETY	           = 75,
        MODE	           = 76,
        DISTANCE	       = 77,
        WORKOUT_CONTROL	   = 81   
    }

    public enum REGISTER : byte
    {
        KPH                      =   0,  // SpeedConverter           ReadOnly: false     - Target speed, hundredths (160 = 1.60 km/h). The ONLY usable speed value on this board since REG_ACTUAL_KPH returns always 0
        GRADE                    =   1,  // GradeConverter           ReadOnly: false     - Target incline, hundredths (1000 = 10.00 %)
        RESISTANCE               =   2,  // ResistanceConverter      ReadOnly: false  
        WATTS                    =   3,  // ShortConverter           ReadOnly: true   
        CURRENT_DISTANCE         =   4,  // IntConverter             ReadOnly: true      - Advances while the belt turns — the reliable "is it moving" signal
        RPM                      =   5,  // ShortConverter           ReadOnly: true   
        DISTANCE                 =   6,  // IntConverter             ReadOnly: true   
        KEY_OBJECT               =   7,  // KeyObjConverter          ReadOnly: true      - Physical button press (code, raw code, timestamp, hold time)
        FAN_SPEED                =   8,  // ByteConverter            ReadOnly: false     - Target fan speed
        VOLUME                   =   9,  // ByteConverter            ReadOnly: false     
        PULSE                    =  10,  // PulseConverter           ReadOnly: false     - Heart Rate
        RUNNING_TIME             =  11,  // IntConverter             ReadOnly: true   
        WORKOUT_MODE             =  12,  // ModeConverter            ReadOnly: false     - the console state machine, 1 byte
        CALORIES                 =  13,  // CaloriesConverter        ReadOnly: true   
        AUDIO_SOURCE             =  14,  // AudioSourceConverter     ReadOnly: false  
        LAP_TIME                 =  15,  // ShortConverter           ReadOnly: true   
        ACTUAL_KPH               =  16,  // SpeedConverter           ReadOnly: true      - Actual Speed. ⚠️ not implemented on this board — always 0
        ACTUAL_INCLINE           =  17,  // GradeConverter           ReadOnly: true      - Actual incline (signed)
        ACTUAL_RESISTANCE        =  18,  // ResistanceConverter      ReadOnly: true   
        ACTUAL_DISTANCE          =  19,  // IntConverter             ReadOnly: true   
        CURRENT_TIME             =  20,  // IntConverter             ReadOnly: true   
        CURRENT_CALORIES         =  21,  // CaloriesConverter        ReadOnly: true   
        GOAL_TIME                =  22,  // IntConverter             ReadOnly: false  
        INTERVAL_KPH             =  23,  // IntervalDoubleConverter  ReadOnly: true   
        AGE                      =  24,  // ByteConverter            ReadOnly: false  
        WEIGHT                   =  25,  // DoubleConverter          ReadOnly: false  
        GEAR                     =  26,  // GearConverter            ReadOnly: false      
        MAX_GRADE                =  27,  // GradeConverter           ReadOnly: true       - Machine MAX INCLINE
        MIN_GRADE                =  28,  // GradeConverter           ReadOnly: true       - Machine MIN INCLINE
        TRANS_MAX                =  29,  // ShortConverter           ReadOnly: false      
        MAX_KPH                  =  30,  // SpeedConverter           ReadOnly: true       - Machine MAX SPEED
        MIN_KPH                  =  31,  // SpeedConverter           ReadOnly: true       - Machine MIN SPEED
        IDLE_TIMEOUT             =  34,  // ShortConverter           ReadOnly: false      - Seconds of Pause. On this machine it reads 120s (Related to the SLEEP State) 
        PAUSE_TIMEOUT            =  35,  // ShortConverter           ReadOnly: false  
        SYSTEM_UNITS             =  36,  // BoolConverter            ReadOnly: false  
        GENDER                   =  37,  // BoolConverter            ReadOnly: false  
        FIRST_NAME               =  38,  // StringConverter          ReadOnly: false  
        LAST_NAME                =  39,  // StringConverter          ReadOnly: false  
        USER_NAME                =  40,  // StringConverter          ReadOnly: false  
        HEIGHT                   =  41,  // ShortConverter           ReadOnly: false  
        MAX_RESISTANCE_LEVEL     =  42,  // ByteConverter            ReadOnly: true   
        MAX_WEIGHT               =  43,  // DoubleConverter          ReadOnly: true   
        WARMUP_DISTANCE          =  44,  // IntConverter             ReadOnly: true   
        WARMUP_TIME              =  45,  // ShortConverter           ReadOnly: true   
        WARMUP_TIMEOUT           =  46,  // ShortConverter           ReadOnly: false  
        WARMUP_CALORIES          =  47,  // CaloriesConverter        ReadOnly: true   
        INTERVAL_GRADE           =  48,  // DoubleConverter          ReadOnly: true   
        MAX_PULSE                =  49,  // ByteConverter            ReadOnly: true   
        WT_MAX_KPH               =  51,  // SpeedConverter           ReadOnly: true   
        AVERAGE_GRADE            =  52,  // GradeConverter           ReadOnly: true   
        WT_MAX_GRADE             =  53,  // GradeConverter           ReadOnly: true   
        AVERAGE_WATTS            =  54,  // ShortConverter           ReadOnly: true   
        MAX_WATTS                =  55,  // ShortConverter           ReadOnly: true   
        AVERAGE_RPM              =  56,  // ShortConverter           ReadOnly: true   
        MAX_RPM                  =  57,  // ShortConverter           ReadOnly: true   
        KPH_GOAL                 =  58,  // SpeedConverter           ReadOnly: false  
        GRADE_GOAL               =  59,  // GradeConverter           ReadOnly: false  
        RESISTANCE_GOAL          =  60,  // ResistanceConverter      ReadOnly: false  
        WATT_GOAL                =  61,  // ShortConverter           ReadOnly: false  
        RPM_GOAL                 =  63,  // ShortConverter           ReadOnly: false  
        DISTANCE_GOAL            =  64,  // IntConverter             ReadOnly: false  
        PULSE_GOAL               =  65,  // ByteConverter            ReadOnly: false  
        START_UP_TIME            =  66,  // IntConverter             ReadOnly: true   
        BELT_TOTAL_TIME          =  67,  // IntConverter             ReadOnly: true      - Lifetime Machine's Belt total time 
        BELT_TOTAL_METERS        =  68,  // IntConverter             ReadOnly: true      - Lifetime Machine's Belt total distance
        MOTOR_TOTAL_DISTANCE     =  69,  // IntConverter             ReadOnly: true   
        TOTAL_TIME               =  70,  // IntConverter             ReadOnly: true   
        COOL_DOWN_TIMEOUT        =  71,  // ShortConverter           ReadOnly: false  
        COOL_DOWN_TIME           =  72,  // ShortConverter           ReadOnly: true   
        COOL_DOWN_DISTANCE       =  73,  // IntConverter             ReadOnly: true   
        COOL_DOWN_CALORIES       =  74,  // CaloriesConverter        ReadOnly: true   
        VERTICAL_METER_NET       =  75,  // VerticalMeterConverter   ReadOnly: true   
        VERTICAL_METER_GAIN      =  76,  // VerticalMeterConverter   ReadOnly: true   
        REPS                     =  77,  // ShortConverter           ReadOnly: true   
        LEFT_REPS                =  78,  // ShortConverter           ReadOnly: true   
        RIGHT_REPS               =  79,  // ShortConverter           ReadOnly: true   
        REP_LENGTH               =  80,  // ShortConverter           ReadOnly: true   
        REP_LEFT_LENGTH          =  81,  // ShortConverter           ReadOnly: true   
        REP_RIGHT_LENGTH         =  82,  // ShortConverter           ReadOnly: true   
        BURN_RATE                =  83,  // BurnRateConverter        ReadOnly: false  
        AVG_BURN_RATE            =  84,  // BurnRateConverter        ReadOnly: true   
        MAX_BURN_RATE            =  85,  // BurnRateConverter        ReadOnly: true   
        INTERVAL_RPM             =  86,  // IntervalIntConverter     ReadOnly: true   
        INTERVAL_RESISTANCE      =  87,  // IntervalIntConverter     ReadOnly: true   
        GOAL_CALORIES            =  94,  // CaloriesConverter        ReadOnly: false  
        IDLE_MODE_LOCKOUT        =  95,  // BoolConverter            ReadOnly: false    -- Reads 0 on this machine (Related to the SLEEP State) 
        START_REQUESTED          =  96,  // BoolConverter            ReadOnly: true   
        FAN_STATE                =  98,  // FanStateConverter        ReadOnly: false  
        ACTIVATION_LOCK          = 100,  // ByteConverter            ReadOnly: false  
        PAUSED_TIME              = 103,  // IntConverter             ReadOnly: true   
        SLEEP_TIMER_STATE        = 107,  // BoolConverter            ReadOnly: false    -- Not Supported on this machine (Related to the SLEEP State) 
        REQUIRE_START_REQUESTED  = 108,  // BoolConverter            ReadOnly: false  
        STROKES                  = 109,  // ShortConverter           ReadOnly: true   
        STROKES_PER_MIN          = 110,  // ByteConverter            ReadOnly: true   
        FIVE_HUNDRED_SPLIT       = 111,  // ShortConverter           ReadOnly: true   
        AVG_FIVE_HUNDRED_SPLIT   = 112,  // ShortConverter           ReadOnly: true   
        IS_CLUB_UNIT             = 115,  // BoolConverter            ReadOnly: true   
        IS_READY_TO_DISCONNECT   = 116,  // BoolConverter            ReadOnly: true   
        IS_CONSTANT_WATTS_MODE   = 119,  // BoolConverter            ReadOnly: false    
    }
    
    public enum STATUS : byte
    {
        UNKNOWN       = 0,   // When the app starts, indicates that some checking needs to be tone to see the board status
        DISCONNECTED  = 1,   // Board is not connected
        CONNECTING    = 2,   // Trying to connect to the board
        HANDSHAKING   = 3,   // Board needs the handshake (including the 32 bit challenge)
        CONNECTED     = 4,   // Board is connected
        DISCONNECTING = 5,    // Requested Board disconnection
        ERROR         = 6    // Got some answer thar indicates that we sent wrong data.
    }

    public enum MODE : byte
    {
        UNKNOWN         =  0,   //When the app started, indicates some check needs to be done to see the belt status
        IDLE            =  1,   //Machine waits for user's input
        RUNNING         =  2,   //Belt is moving
        PAUSE           =  3,   //Belt is stopped, workout is still running
        RESULTS         =  4,   //
        DEBUG           =  5,
        LOG             =  6,
        MAINTENANCE     =  7,
        DMK             =  8,   //SAFETY KEY OUT
        DEMO            =  9,
        WARM_UP         = 10,   //Board is going from 0 (actually 1.6km/h) to desired speed in 10 seconds
        COOL_DOWN       = 11,   //Board is going from actual speed to 0 in 10 secods
        SLEEP           = 12,
        RESUME          = 13,    //Resume belt movement and workout
        LOCKED          = 14,
        PAUSE_OVERRRIDE = 15
    }

    public enum KEYS : byte
    {
        KEY_NONE                = 0x00,
        KEY_EMERGENCY           = 0x00,  //NOT USED 
        KEY_START               = 0x02,
        KEY_STOP                = 0x01,
        KEY_INCLINE_UP          = 0x05,
        KEY_INCLINE_DOWN        = 0x06,
        KEY_SPEED_UP            = 0x03,
        KEY_SPEED_DOWN          = 0x04,
        KEY_FAN_UP              = 0x32,
        KEY_FAN_DOWN            = 0x33,
        KEY_GRADE_12            = 0x00, 
        KEY_GRADE_10            = 0x00, 
        KEY_GRADE_08            = 0x00,
        KEY_GRADE_06            = 0x00,
        KEY_GRADE_05            = 0x00,
        KEY_GRADE_04            = 0x00,
        KEY_GRADE_03            = 0x00,
        KEY_GRADE_02            = 0x00,
        KEY_GRADE_01            = 0x00,
        KEY_GRADE_00            = 0x00,
        KEY_GRADE_01_NEG        = 0x00,
        KEY_GRADE_02_NEG        = 0x00,
        KEY_GRADE_03_NEG        = 0x00,
        KEY_SPEED_12            = 0x00,
        KEY_SPEED_11            = 0x00,
        KEY_SPEED_10            = 0x00,        
        KEY_SPEED_09            = 0x00,        
        KEY_SPEED_08            = 0x00,
        KEY_SPEED_07            = 0x00,
        KEY_SPEED_06            = 0x00,
        KEY_SPEED_05            = 0x00,
        KEY_SPEED_04            = 0x00,
        KEY_SPEED_03            = 0x00,
        KEY_SPEED_02            = 0x00,
        KEY_SPEED_01            = 0x00
    }

    public STATUS Status()
    {
        return _status;
    }

    public MODE Mode()
    {
        return _mode;
    }

    void ChangeBoardStatus(STATUS newStatus)
    {
        if (_status != newStatus)
        {
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "ChangeBoardStatus", $"Request to change board status from '{_status.ToString()}' to '{newStatus.ToString()}'.");
            _status = newStatus;
        }
    }

    void ChangeBeltMode(Board.MODE newMode)
    {
        if (_mode != newMode)
        {
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "ChangeBeltMode",  $"Request to change belt mode from '{_mode.ToString()}' to '{newMode.ToString()}'.");
            _mode = newMode;
        }

    }

    public void DumpHidDevices()
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "DumpHidDevices", $"Searching for HID devices:");
        foreach (HidDevice dev in DeviceList.Local.GetHidDevices())
        {
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "DumpHidDevices", $"   VendorID: '{dev.VendorID:X4}' (Manufacturer: '{dev.GetManufacturer()}'), ProductID: '{dev.ProductID:X4}'  Product: '{dev.GetProductName()}') on '{dev.DevicePath}'");
        }
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "DumpHidDevices", $"Done.");
    }
    
    public void Open()
        {
            if (_device == null)
            {
                if (!_keepTryingSiently)
                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Open", $"Searching for device (VendorID {VID:X4}, ProductID {PID:X4})...");
                
                _device = DeviceList.Local.GetHidDevices(VID, PID).FirstOrDefault()!;
                if (_device == null)
                {
                    if (!_keepTryingSiently)
                    {
                        Helper.Log(Helper.LEVELS.ERROR, moduleName, "Open", "   Device not found. Is it plugged in?");
                        ChangeBoardStatus(Board.STATUS.DISCONNECTED);
                        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Open", "      Will keep trying to find the device silently...");
                    }
                    
                    _keepTryingSiently = true;
                    return;
                }
            }

            if (_hidStream == null)
            {
                if (!_keepTryingSiently)
                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Open", "Trying to open Device...");
                
                ChangeBoardStatus(Board.STATUS.CONNECTING);
                _hidStream = _device?.Open()!;
                if (_hidStream == null)
                {
                    if (!_keepTryingSiently){
                        Helper.Log(Helper.LEVELS.ERROR, moduleName, "Open", "   Device could not be open. Is it opened by another process?");
                        ChangeBoardStatus(Board.STATUS.DISCONNECTED);
                        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Open", "      Will keep trying to open the device silently...");
                    }
                        
                    _keepTryingSiently = true;
                    return;
                }

                _hidStream.ReadTimeout = READ_TIMEOUT;
                _hidStream.WriteTimeout = WRITE_TIMEOUT;
                _outLen = _device!.GetMaxOutputReportLength(); 
                _inLen = _device!.GetMaxInputReportLength();  
                
                Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Open", $"   Device (VendorID {VID:X4}, ProductID {PID:X4}, OutcomingReportSize {_outLen}, IncomingReportSize {_inLen}, Path {_device.DevicePath}) open!");
                ChangeBoardStatus(Board.STATUS.HANDSHAKING);
            }

            _keepTryingSiently = false;

        }

   public void Dispose()
    { 
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Dispose", "Disposing...");
        
        if (_hidStream == null)
            return;

        try
        {
            ChangeBoardStatus(Board.STATUS.DISCONNECTING);
            
            _hidStream.ReadTimeout = 50;
            var buf = new byte[_inLen];
            for (int i = 0; i < 8; i++)
            {
                _hidStream.Read(buf, 0, buf.Length);
            }

            _hidStream.Close();
        }
        catch(Exception e)
        {
            raiseError($"Error while draining unread replies: '{e.Message.ToString()}'!");
        }

        try
        {
            _hidStream.Dispose();
        }
        catch (Exception e)
        {
            Helper.Log(Helper.LEVELS.ERROR, moduleName, "Dispose", "Error disposing HID stream: " + e.Message.ToString());
        }

        ChangeBoardStatus(Board.STATUS.DISCONNECTED);
        _hidStream = null!;

    }
    
    //------------------------------------------------------------------------
    private void raiseError(string msg)
    {
        _lastErrorStatus = msg;
        Helper.Log(Helper.LEVELS.ERROR, moduleName, "raiseError", msg);
        ChangeBoardStatus(STATUS.ERROR);
    }

    public string lastErrorMsg()
    {
        return _lastErrorStatus;
    }
    
    private string getFormatedFrame(byte[] frame)
    {   
        //'02 04 81 87 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF'
        string sBuffer = BitConverter.ToString(frame).Replace('-', ' ');
        string sShortBuffer =  sBuffer.Substring(0, (frame[1] * 3)) ;
        return "[" + (sBuffer.Length >= sShortBuffer.Length ? sShortBuffer + "..." : sBuffer ) + "]";
    }
    
    private string getFrameExplanation(byte[] frame)
    {   
        //'02 04 81 87 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF'
        string sBuffer = BitConverter.ToString(frame).Replace('-', ' ');
        string sShortBuffer =  sBuffer.Substring(0, (frame[1] * 3)) ;
        string result = (sBuffer.Length >= sShortBuffer.Length ? sShortBuffer + "..." : sBuffer);

       // reply: [04 07 02 02 00 00 0F]
       //         │  │  │  │  └──┴─────  ActualKph = 0.00 km/h (belt stopped)
       //         │  │  │  └──────────── Status
       //         │  │  └─────────────── ReadWriteData
       //         │  └────────────────── Length 7
       //         └───────────────────── Device TREADMILL
        
        //string details = "";
        //for (int i = 0; i < sShortBuffer.Length ; i++)
        // {
        //    
        // }

        return result;
    }

    private byte checksum(byte[] buffer)
    {
        int sum = 0;
        int len = buffer[1];
        for (int i = 0; i < len - 1; i++) sum += buffer[i];
        return (byte) sum;  
    }
    
    private void send(COMMAND command, byte[] buffer)
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "send", $"Sending '{command.ToString()}' command. Frame is '{getFormatedFrame(buffer)}'.");
        _hidStream.Write(buffer);
    }

    private byte[] read(COMMAND command)
    {
        byte[] fullBuffer = new byte[_inLen];
        
        //Since we might be reading trash, or partial answers, we keep trying until we get an answer
        for (int tries = 0; tries < MAX_READING_RETRIES; tries++)
        {
            int bufferSize = _hidStream.Read(fullBuffer, 0, fullBuffer.Length);
            if (bufferSize > 6) // why 6?
            {
                if (bufferSize <= _inLen)
                {   
                    //Keep only the usefull part of the buffer 
                    byte[] shortBuffer = new byte[bufferSize];
                    Array.Copy(fullBuffer, shortBuffer, bufferSize);
                    
                    //Check if this is the answer we are looking for
                    if (shortBuffer[2] == (byte) command)
                    {
                        //This is our answer!
                        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "read", $"Received '{command.ToString()}' command. Frame is '{getFormatedFrame(shortBuffer)}'. Status is '{shortBuffer[3]}' ({(shortBuffer[3] == 2 ? "OK" : "FAILED")})!");
                        return shortBuffer;
                    }
                    else
                    {
                        Helper.Log(Helper.LEVELS.WARN, moduleName, "read", $"Ignoring response {getFormatedFrame(shortBuffer)}. Command ID Mismatch!");
                    }
                } 
            }    
        }
        
        Helper.Log(Helper.LEVELS.ERROR, moduleName, "read", $"Unable to read a response for '{command.ToString()}' command!");
        return null!;
    }

    private byte getDeviceID(COMMAND command)
    {
        byte deviceID = (byte) DEVICE.NONE;
        
        switch(command)
        {
            case COMMAND.DEVICE_INFO:
            case COMMAND.VERSION_INFO:
            case COMMAND.SYSTEM_INFO: 
            case COMMAND.SERIAL_NUMBER: 
                deviceID = (byte) DEVICE.MAIN; //0x00
                break;
            
            case COMMAND.SUPPORTED_DEVICES: 
            case COMMAND.SUPPORTED_COMMANDS:
            case COMMAND.VERIFY_SECURITY:   
            case COMMAND.READ_WRITE: 
                deviceID = (byte) DEVICE.TREADMILL; //0x04
                break;

            case COMMAND.NONE:                    
            case COMMAND.PORTAL_DEV_LISTEN:	      
            case COMMAND.TEST:                    
            case COMMAND.CONNECT:                 
            case COMMAND.DISCONNECT:              
            case COMMAND.CALIBRATE:               
            case COMMAND.UPDATE:                  
            case COMMAND.ENTER_BOOT_LOADER:       
            case COMMAND.TASK_INFO:               
            case COMMAND.READ_CONFIG:             
            case COMMAND.SET_TESTING_KEY:         
            case COMMAND.SET_TESDTING_TACH:       
            case COMMAND.SPEED_GRADE_LIMIT:       
            case COMMAND.PROTOCOL_DATA:           
            case COMMAND.RAW:                     
                Helper.Log(Helper.LEVELS.ERROR, moduleName, "createFrame", $"Failed to identify device for command '{command.ToString()}'. Have you tried to list SUPPORTED_DEVICES?");
                deviceID = (byte) DEVICE.NONE;
                break;
        }

        return deviceID;
    }
    
    private byte[] createFrame(COMMAND command, byte[] payload)
    {
        // A Frame is a byte array of up to _outLen bytes (returned after opened the device) where:
        //    [dev]        Device ID (DEVICES enum)
        //    [len]        Total lenght of the frame (including [addr, len, cmd and checksum)
        //    [cmd]        Command (COMMAND enum)
        //    [payload…]   
        //    [checksum]   8 bit sum of every preceding byte in the frame

        //Creates the frame 
        byte[] frame = new Byte[_outLen];
        
        //Fill the frame with the "unused bytes" (0xFF)
        Array.Fill(frame, (byte) 0xFF);
        
        //Calculate the len
        byte len = (byte) (payload.Length + 4);  //+4 because the 4 bytes ([add], [cmd], [len] and [checksum]) also count.
        if (payload.Length > _outLen - 4)
        {
            Helper.Log(Helper.LEVELS.ERROR, moduleName, "createFrame", $"Failed to create a '{command.ToString()}' frame because payload size would result in a frame bigger then {_outLen} bytes (REPORT_SIZE).");
            return null!;
        }

        //Add the deviceID
        frame[0] = getDeviceID(command);

        //Add the Len 
        frame[1] = len; 
        
        //Add the command 
        frame[2] = (byte) command;
        
        //Copy the payload to the frame
        Array.Copy(payload, 0, frame, 3, payload.Length);
        
        //Add checksum (it will check the len on byte 1)
        frame[len - 1] = checksum(frame); 
        
        //Return the frame
        return frame;

    }
    
    public byte[]  sendCommand(COMMAND command, byte[] payload = null!)
    {
        byte[] outFrame = new byte[_outLen];
        byte[] inFrame = null!;

        switch (command)
        {
            case COMMAND.DEVICE_INFO:
                //18:47:32.508  TX  DeviceInfo    | 02 04 81 87
                //18:47:32.519  RX  DeviceInfo  status=0x02 (OK)  payload[24]=54 01 01 00 00 00 00 FF 0F DB FF FB DB FC FB FB 0C E7 1F 00 C0 D5 10 18

                outFrame = createFrame(command, new byte[0]);
                send(command, outFrame);
                inFrame = read(command);
                if (inFrame[3] == 2)
                {
                    //TODO Handle the result
                    _deviceInfo = inFrame;
                        
                    _softwareVersion = 0;
                    _serialNumber = 0;
                    
                    return _deviceInfo;
                }
                else
                {
                    raiseError($"Received an ERROR status after sending command '{command.ToString()}'!");
                }
                break;
            
            case COMMAND.SUPPORTED_DEVICES:
                //18:47:32.519  TX  SupportedDevices    | 04 04 80 88
                //18:47:32.521  RX  SupportedDevices  status=0x02 (OK)  payload[12]=0B 4C 47 43 4D 4E 4A 40 46 4F 41 42
                break;
            
            case COMMAND.SUPPORTED_COMMANDS:
                //18:47:32.521  TX  SupportedCommands    | 04 04 88 90
                //18:47:32.523  RX  SupportedCommands  status=0x02 (OK)  payload[8]=09 82 90 83 71 89 84 95
                break;
            
            case COMMAND.SYSTEM_INFO:
                //18:47:32.523  TX  SystemInfo (2 byte payload)    | 02 06 82 00 00 8A
                //18:47:32.525  RX  SystemInfo  status=0x02 (OK)  payload[28]=64 00 01 2B 37 00 00 84 D1 06 00 E8 03 2B 7D 00 00 6C DC 02 64 00 00 00 01 02 00 00
    
                //TODO:  
                //_systemInfo = ?
                //_model = 0;
                //_partNumber = 0;
                break;
            
            case COMMAND.VERSION_INFO:
                //18:47:32.525  TX  VersionInfo (2 byte payload)    | 02 06 84 00 00 8C
                //18:47:32.527  RX  VersionInfo  status=0x02 (OK)  payload[23]=54 50 00 30 2E 31 2E 32 33 34 35 36 37 38 39 2E 30 31 32 33 01 2A 03
                //TODO:
                //_versionInfo
                //_masterLibraryVersion
                break;
            
            case COMMAND.VERIFY_SECURITY:   
                //18:47:32.531  TX  VerifySecurity (36 byte payload)    | 04 28 90 07 5A 84 BC EE 26 50 98 C2 02 4C 84 C6 1E 58 B0 FA 2A 94 CC 3E 96 C0 28 92 F2 5C B4 16 6E C8 60 A0 02 00 00 FB
                //18:47:32.533  RX  VerifySecurity  status=0x02 (OK)  payload[1]=08
                break;   

            default:
                Helper.Log(Helper.LEVELS.DEBUG, moduleName, "sendCommand", $"COMMAND '{command.ToString()}' WAS NOT IMPLEMENTED YET!.");
                break;
        }

        return null!;
    }
    
    //-------------------------------------------------------------------------
    
    
    public void Handshake()
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Handshake", "Starting handshake protocol...");
        try
        {
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Handshake", "   1) Asking for DEVICE_INFO...");
            _deviceInfo = sendCommand(COMMAND.DEVICE_INFO);

            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Handshake", "   2) Asking For SUPPORTED_DEVICES...");
            _supportedDevices = sendCommand(COMMAND.SUPPORTED_DEVICES);
            
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Handshake", "   3) Asking for SUPPORTED_COMMANDS...");
            _suportedComands = sendCommand(COMMAND.SUPPORTED_COMMANDS);
            
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Handshake", "   4) Asking for SYSTEM_INFO...");
            _systemInfo = sendCommand(COMMAND.SYSTEM_INFO, new byte[] { 0, 0 });
             
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Handshake", "   5) Asking for VERSION_INFO...");
            _versionInfo = sendCommand(COMMAND.VERSION_INFO, new byte[] { 0, 0 });
            
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Handshake", "   6) Asking for VERIFY_SECURITY...");
            _securityInfo = sendCommand(COMMAND.DEVICE_INFO);
            
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Handshake", "Handshake accepted successfully."); 
            ChangeBoardStatus(STATUS.CONNECTED);
            
        /*
        var di = Xfer(Main.Frame(Main.CmdDeviceInfo));
        var sd = Xfer(Main.Frame(Main.CmdSupportedDevices));
        var sc = Xfer(Main.Frame(Main.CmdSupportedCommands));
        var si = Xfer(Main.Frame(Main.CmdSystemInfo, new byte[] { 0, 0 }));
        var vi = Xfer(Main.Frame(Main.CmdVersionInfo, new byte[] { 0, 0 }));
        var sn = Xfer(Main.Frame(Main.CmdSerialNumber));

        int softwareVersion = di.Payload.Length > 0 ? di.Payload[0] : 0;                          // DeviceInfo byte 4
        uint serialNumber = di.Payload.Length >= 6 ? BitConverter.ToUInt32(di.Payload, 2) : 0;    // DeviceInfo uint32 @6
        uint model = si.Payload.Length >= 7 ? BitConverter.ToUInt32(si.Payload, 3) : 0;           // SystemInfo uint32 @7
        uint partNumber = si.Payload.Length >= 11 ? BitConverter.ToUInt32(si.Payload, 7) : 0;     // SystemInfo uint32 @11
        int masterLibraryVersion = vi.Payload.Length > 0 ? vi.Payload[0] : 0;                     // VersionInfo byte 4
        
        var payload = Main.ComputeVerifySecurity(serialNumber, partNumber, model, masterLibraryVersion);
        var vs = Xfer(Main.VerifySecurityFrame(payload));        
        */
            
        }              
        catch (Exception e)
        {
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Handshake", "Error while executing handshake protocol:" + e.Message.ToString());  
            ChangeBoardStatus(Board.STATUS.DISCONNECTED);
        }
    }
    
    //-------------------------------------------------------------------------
    // Functions not implemented:

    public  byte[] ComputeVerifySecurity(uint serialNumber, uint partNumber, uint model, int masterLibraryVersion)
    {        
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "ComputeVerifySecurity", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
        var payload = new byte[36];
        /*
        // Documented fix-up for one production run whose part number doesn't match its own hash.
        if (partNumber == 370357 && model == 39915) partNumber = 374677;

        var hash = new byte[32];
        for (int b = 0; b < 32; b++)
        {
            hash[b] = (byte)((b + 1) & 0xFF);
            if (((serialNumber >> b) & 1) != 0)
            {
                uint p = b < 16
                    ? RotateLeft(partNumber, 16) >> b     // >>> in the source = unsigned shift, which C#'s uint >> already is
                    : partNumber >> b;
                hash[b] ^= (byte)(p & 0xFF);
            }
            else
            {
                hash[b] ^= (byte)((hash[b] * (b + model)) & 0xFF);
            }
        }

        var payload = new byte[36];
        Array.Copy(hash, payload, 32);
        int mlv8 = 8 * masterLibraryVersion;
        payload[32] = (byte)(mlv8 & 0xFF);
        payload[33] = (byte)((mlv8 >> 8) & 0xFF);
        payload[34] = (byte)((mlv8 >> 16) & 0xFF);
        payload[35] = (byte)((mlv8 >> 24) & 0xFF);
        
                */
        return payload;

    }

    private void readRegister(REGISTER register)
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "readRegister", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
        //    Read — note the leading zero:
        //    [0x00] [maskLen] [mask bytes, LSB-first]
    }
    
    private void writeRegister(REGISTER register, byte newValue)
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "writeRegister", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
        //    Write — no leading byte at all:
        //    [maskLen] [mask bytes, LSB-first] [values…]
    }
    
    private void SpeedConverter()
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "SpeedConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void GradeConveter()     {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "GradeConveter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void ShortConverter()    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "ShortConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void IntConverter()    
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "IntConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void KeyObjConverter()     
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "KeyObjConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void ByteConverter()    
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "ByteConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void PulseConverter()    
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "PulseConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void ModeConverter()     
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "ModeConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void CaloriesConverter()     
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "CaloriesConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void AudioSourceConverter()    
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "AudioSourceConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void IntervalDoubleConverter()    
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "IntervalDoubleConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void BoolConverter()     
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "BoolConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void VerticalMeterConverter()    
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "VerticalMeterConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void BurnRateConverter()    
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "BurnRateConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    private void FanStateConverter()    
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "FanStateConverter", "PROCEDURE WAS NOT IMPLEMENTED YET!");
        //TODO
    }
    
}
