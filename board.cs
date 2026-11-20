
using System.Text;
using HidSharp;

//  The tablet is detected as            VID: 0E8D (Mediatek) PID:0003 (Mediatek USB Port) 15 for seconds. This is the BROM mode
//  The tablet is sometimes detected as  VID: 0E8D (Mediatek) PID:2000 (Mediatek Preloader USB VCOM). This is a MT6583 preloader mode
//  The tablet is sometimes detected as  VID: 0E8D (Mediatek) PID:2001 (Mediatek DA USB VCOM) 

sealed class Board : IDisposable
{
    private string moduleName = "board.cs";

    private const int VID = 0x213C;             // ICON Health & Fitness (Rebranded as iFit Health & Fitness).
    private const int PID = 0x0002;             // ICON Generic HID (Good Wolf Universal ZH0058).

    private const int READ_TIMEOUT = 500;       // Timeout on Read operations (Default 500ms).
    private const int WRITE_TIMEOUT = 500;      // Timeout on Write operations (Default 500ms).

    private const byte REPORT_ID = 0;           // ReportID will be 0 on both outReports and inReports.
    private const byte STATUS_OK = 2;           // Value (from STATUS byte) (from STATUS byte of an inReport) that indicates OK.
    private const byte STATUS_LOCKED = 8;       // Value (from STATUS byte of an inReport) that indicates we need to perform the challenge again.

    public enum COMMAND : byte
    {
        NONE = 0,                   // NOT USED
        PORTAL_DEV_LISTEN = 1,      // NOT USED
        READ_WRITE = 2,             // Basically everything is performed using this one.
        TEST = 3,                   // NOT USED
        CONNECT = 4,                // NOT USED
        DISCONNECT = 5,             // NOT USED
        CALIBRATE = 6,              // NOT USED
        UPDATE = 9,                 // NOT USED. Firmware Operation, ⚠️ DO NOT USE IT!!!!!!!!!!!
        ENTER_BOOT_LOADER = 56,     // NOT USED. Firmware Operation, ⚠️ DO NOT USE IT!!!!!!!!!!!
        SET_TESTING_KEY = 112,      // NOT USED
        SET_TESDTING_TACH = 113,    // NOT USED
        SUPPORTED_DEVICES = 128,    // Lists board's subsystems
        DEVICE_INFO = 129,          // Lists board's capabilities bitmap
        SYSTEM_INFO = 130,          // Lists board's parameters
        TASK_INFO = 131,            // NOT USED
        VERSION_INFO = 132,         // NOT USED
        SUPPORTED_COMMANDS = 136,   // Lists board's supported commands
        READ_CONFIG = 137,          // NOT USED.
        VERIFY_SECURITY = 144,      // 32 Byte challenge. Board replies 08
        PROTOCOL_DATA = 145,        // NOT USED
        SPEED_GRADE_LIMIT = 146,    // NOT USED
        SERIAL_NUMBER = 149,        // ASCII Brainboard serial number
        RAW = 255                   // NOT USED
    }

    public enum DEVICE : byte
    {
        NONE = 0,
        MULTIPLE_DEVICES = 1,
        MAIN = 2,
        PORTAL = 3,
        TREADMILL = 4,
        INCLINE_TRAINER = 5,
        ELLIPTICAL = 6,
        ROWER = 20,
        HEART_RATE_MONITOR = 55,
        AUDIO = 64,
        SPEED = 65,
        GRADE = 66,
        WATTS = 67,
        PULSE = 70,
        KEY_PRESS = 71,
        FAN = 74,
        SAFETY = 75,
        MODE = 76,
        DISTANCE = 77,
        WORKOUT_CONTROL = 81
    }

    public enum REGISTER : int
    {
        KPH = 0,                        // SpeedConverter           ReadOnly: false     - Target speed, hundredths (160 = 1.60 km/h). The ONLY usable speed value on this board since REG_ACTUAL_KPH returns always 0
        GRADE = 1,                      // GradeConverter           ReadOnly: false     - Target incline, hundredths (1000 = 10.00 %)
        RESISTANCE = 2,                 // ResistanceConverter      ReadOnly: false  
        WATTS = 3,                      // ShortConverter           ReadOnly: true   
        CURRENT_DISTANCE = 4,           // IntConverter             ReadOnly: true      - Advances while the belt turns — the reliable "is it moving" signal
        RPM = 5,                        // ShortConverter           ReadOnly: true   
        DISTANCE = 6,                   // IntConverter             ReadOnly: true   
        KEY_OBJECT = 7,                 // KeyObjConverter          ReadOnly: true      - Physical button press (code, raw code, timestamp, hold time)
        FAN_SPEED = 8,                  // ByteConverter            ReadOnly: false     - Target fan speed
        VOLUME = 9,                     // ByteConverter            ReadOnly: false     
        PULSE = 10,                     // PulseConverter           ReadOnly: false     - Heart Rate
        RUNNING_TIME = 11,              // IntConverter             ReadOnly: true   
        WORKOUT_MODE = 12,              // ModeConverter            ReadOnly: false     - the console state machine, 1 byte
        CALORIES = 13,                  // CaloriesConverter        ReadOnly: true   
        AUDIO_SOURCE = 14,              // AudioSourceConverter     ReadOnly: false  
        LAP_TIME = 15,                  // ShortConverter           ReadOnly: true   
        ACTUAL_KPH = 16,                // SpeedConverter           ReadOnly: true      - Actual Speed. ⚠️ not implemented on this board — always 0
        ACTUAL_INCLINE = 17,            // GradeConverter           ReadOnly: true      - Actual incline (signed)
        ACTUAL_RESISTANCE = 18,         // ResistanceConverter      ReadOnly: true   
        ACTUAL_DISTANCE = 19,           // IntConverter             ReadOnly: true   
        CURRENT_TIME = 20,              // IntConverter             ReadOnly: true   
        CURRENT_CALORIES = 21,          // CaloriesConverter        ReadOnly: true   
        GOAL_TIME = 22,                 // IntConverter             ReadOnly: false  
        INTERVAL_KPH = 23,              // IntervalDoubleConverter  ReadOnly: true   
        AGE = 24,                       // ByteConverter            ReadOnly: false  
        WEIGHT = 25,                    // DoubleConverter          ReadOnly: false  
        GEAR = 26,                      // GearConverter            ReadOnly: false      
        MAX_GRADE = 27,                 // GradeConverter           ReadOnly: true       - Machine MAX INCLINE
        MIN_GRADE = 28,                 // GradeConverter           ReadOnly: true       - Machine MIN INCLINE
        TRANS_MAX = 29,                 // ShortConverter           ReadOnly: false      
        MAX_KPH = 30,                   // SpeedConverter           ReadOnly: true       - Machine MAX SPEED
        MIN_KPH = 31,                   // SpeedConverter           ReadOnly: true       - Machine MIN SPEED
        IDLE_TIMEOUT = 34,              // ShortConverter           ReadOnly: false      - Seconds of Pause. On this machine it reads 120s (Related to the SLEEP State) 
        PAUSE_TIMEOUT = 35,             // ShortConverter           ReadOnly: false  
        SYSTEM_UNITS = 36,              // BoolConverter            ReadOnly: false  
        GENDER = 37,                    // BoolConverter            ReadOnly: false  
        FIRST_NAME = 38,                // StringConverter          ReadOnly: false  
        LAST_NAME = 39,                 // StringConverter          ReadOnly: false  
        USER_NAME = 40,                 // StringConverter          ReadOnly: false  
        HEIGHT = 41,                    // ShortConverter           ReadOnly: false  
        MAX_RESISTANCE_LEVEL = 42,      // ByteConverter            ReadOnly: true    
        MAX_WEIGHT = 43,                // DoubleConverter          ReadOnly: true   
        WARMUP_DISTANCE = 44,           // IntConverter             ReadOnly: true   
        WARMUP_TIME = 45,               // ShortConverter           ReadOnly: true   
        WARMUP_TIMEOUT = 46,            // ShortConverter           ReadOnly: false  
        WARMUP_CALORIES = 47,           // CaloriesConverter        ReadOnly: true   
        INTERVAL_GRADE = 48,            // DoubleConverter          ReadOnly: true   
        MAX_PULSE = 49,                 // ByteConverter            ReadOnly: true   
        WT_MAX_KPH = 51,                // SpeedConverter           ReadOnly: true   
        AVERAGE_GRADE = 52,             // GradeConverter           ReadOnly: true   
        WT_MAX_GRADE = 53,              // GradeConverter           ReadOnly: true   
        AVERAGE_WATTS = 54,             // ShortConverter           ReadOnly: true   
        MAX_WATTS = 55,                 // ShortConverter           ReadOnly: true   
        AVERAGE_RPM = 56,               // ShortConverter           ReadOnly: true   
        MAX_RPM = 57,                   // ShortConverter           ReadOnly: true   
        KPH_GOAL = 58,                  // SpeedConverter           ReadOnly: false  
        GRADE_GOAL = 59,                // GradeConverter           ReadOnly: false  
        RESISTANCE_GOAL = 60,           // ResistanceConverter      ReadOnly: false  
        WATT_GOAL = 61,                 // ShortConverter           ReadOnly: false  
        RPM_GOAL = 63,                  // ShortConverter           ReadOnly: false  
        DISTANCE_GOAL = 64,             // IntConverter             ReadOnly: false  
        PULSE_GOAL = 65,                // ByteConverter            ReadOnly: false  
        START_UP_TIME = 66,             // IntConverter             ReadOnly: true   
        BELT_TOTAL_TIME = 67,           // IntConverter             ReadOnly: true      - Lifetime Machine's Belt total time 
        BELT_TOTAL_METERS = 68,         // IntConverter             ReadOnly: true      - Lifetime Machine's Belt total distance
        MOTOR_TOTAL_DISTANCE = 69,      // IntConverter             ReadOnly: true   
        TOTAL_TIME = 70,                // IntConverter             ReadOnly: true   
        COOL_DOWN_TIMEOUT = 71,         // ShortConverter           ReadOnly: false  
        COOL_DOWN_TIME = 72,            // ShortConverter           ReadOnly: true   
        COOL_DOWN_DISTANCE = 73,        // IntConverter             ReadOnly: true   
        COOL_DOWN_CALORIES = 74,        // CaloriesConverter        ReadOnly: true   
        VERTICAL_METER_NET = 75,        // VerticalMeterConverter   ReadOnly: true   
        VERTICAL_METER_GAIN = 76,       // VerticalMeterConverter   ReadOnly: true   
        REPS = 77,                      // ShortConverter           ReadOnly: true   
        LEFT_REPS = 78,                 // ShortConverter           ReadOnly: true   
        RIGHT_REPS = 79,                // ShortConverter           ReadOnly: true   
        REP_LENGTH = 80,                // ShortConverter           ReadOnly: true   
        REP_LEFT_LENGTH = 81,           // ShortConverter           ReadOnly: true   
        REP_RIGHT_LENGTH = 82,          // ShortConverter           ReadOnly: true   
        BURN_RATE = 83,                 // BurnRateConverter        ReadOnly: false  
        AVG_BURN_RATE = 84,             // BurnRateConverter        ReadOnly: true   
        MAX_BURN_RATE = 85,             // BurnRateConverter        ReadOnly: true   
        INTERVAL_RPM = 86,              // IntervalIntConverter     ReadOnly: true   
        INTERVAL_RESISTANCE = 87,       // IntervalIntConverter     ReadOnly: true   
        GOAL_CALORIES = 94,             // CaloriesConverter        ReadOnly: false  
        IDLE_MODE_LOCKOUT = 95,         // BoolConverter            ReadOnly: false    -- Reads 0 on this machine (Related to the SLEEP State) 
        START_REQUESTED = 96,           // BoolConverter            ReadOnly: true   
        FAN_STATE = 98,                 // FanStateConverter        ReadOnly: false  
        ACTIVATION_LOCK = 100,          // ByteConverter            ReadOnly: false  
        PAUSED_TIME = 103,              // IntConverter             ReadOnly: true   
        SLEEP_TIMER_STATE = 107,        // BoolConverter            ReadOnly: false    -- Not Supported on this machine (Related to the SLEEP State) 
        REQUIRE_START_REQUESTED = 108,  // BoolConverter            ReadOnly: false  
        STROKES = 109,                  // ShortConverter           ReadOnly: true   
        STROKES_PER_MIN = 110,          // ByteConverter            ReadOnly: true   
        FIVE_HUNDRED_SPLIT = 111,       // ShortConverter           ReadOnly: true   
        AVG_FIVE_HUNDRED_SPLIT = 112,   // ShortConverter           ReadOnly: true   
        IS_CLUB_UNIT = 115,             // BoolConverter            ReadOnly: true   
        IS_READY_TO_DISCONNECT = 116,   // BoolConverter            ReadOnly: true   
        IS_CONSTANT_WATTS_MODE = 119,   // BoolConverter            ReadOnly: false    
    }

    public enum STATUS : byte
    {
        UNKNOWN = 0,        // When the app starts, indicates that some checking needs to be tone to see the board status
        HANDSHAKING = 1,    // Board needs the handshake (including the 32 bit challenge)
        CONNECTED = 2,      // Board is connected
        DISCONNECTING = 3,  // Requested Board disconnection
        DISCONNECTED = 4,   // Board is not connected
        ERROR = 5           // Got some answer thar indicates that we sent wrong data.
    }

    public enum MODE : int
    {
        UNKNOWN = 0,         // When the app started, indicates some check needs to be done to see the belt status
        IDLE = 1,            // Machine waits for user's input
        RUNNING = 2,         // Belt is moving
        PAUSE = 3,           // Belt is stopped, workout is still running
        RESULTS = 4,         // 
        DEBUG = 5,           // 
        LOG = 6,             // 
        MAINTENANCE = 7,     // 
        DMK = 8,             // SAFETY KEY OUT
        DEMO = 9,            // 
        WARM_UP = 10,        // Board is going from 0 (actually 1.6km/h) to desired speed in 10 seconds
        COOL_DOWN = 11,      // Board is going from actual speed to 0 in 10 secods
        SLEEP = 12,          // 
        RESUME = 13,         // Resume belt movement and workout
        LOCKED = 14,         // 
        PAUSE_OVERRRIDE = 15 // 
    }

    public enum KEYS : byte
    {
        NONE = 0,
        STOP = 1,
        START = 2,
        SPEED_UP = 3,
        SPEED_DOWN = 4,
        INCLINE_UP = 5,
        INCLINE_DOWN = 6,
        FAN_UP = 50,
        FAN_DOWN = 51,
        VOL_UP = 200,
        VOL_DOWN = 201,
        GRADE_03_NEG = 203,
        GRADE_02_NEG = 204,
        GRADE_01_NEG = 205,
        GRADE_00 = 206,
        GRADE_01 = 207,
        GRADE_02 = 208,
        GRADE_03 = 209,
        GRADE_04 = 210,
        GRADE_05 = 211,
        GRADE_06 = 212,
        GRADE_08 = 214,
        GRADE_10 = 216,
        GRADE_12 = 218,
        SPEED_01 = 232,
        SPEED_02 = 233,
        SPEED_03 = 234,
        SPEED_04 = 235,
        SPEED_05 = 236,
        SPEED_06 = 237,
        SPEED_07 = 238,
        SPEED_08 = 239,
        SPEED_09 = 240,
        SPEED_10 = 241,
        SPEED_11 = 242,
        SPEED_12 = 243
    }

    private HidDevice _device = null!;          // The HID Device. 
    private HidStream _hidStream = null!;       // The read/write stream of the HID Device.

    // Last data read from the board------------------------------------------------------------------------------------
    private STATUS _status = STATUS.UNKNOWN;    // The default STATUS of the HID Device.
    private MODE _mode = MODE.UNKNOWN;          // The default MODE of the treadmill's belt.
    private int _outReportSize = 0;             // Size of the outcoming report (reported by the HID after open). 
    private int _inReportSize = 0;              // Size of the incoming report (reported by the HID after open).
    private int _softwareVersion = 0;           // Software version (On my board it is '84')
    private uint _softwareUnknownByte = 0;      // Unknown Byte  (On my board it is '1')
    private uint _model = 0;                    // Model (On my board it is '14123')
    private uint _partNumber = 0;               // Part Number (On my board it is '446852')
    private int _masterLibraryVersion = 0;      // Master Libray Version (On my board it is '84')
    private uint _minSpeed = 0;                 // Minimal Speed the board support (in km/h)  
    private uint _maxSpeed = 0;                 // Maximal Speed the board support (in km/h)  
    private int _currentSpeed = 0;
    private short _minGrade = 0;                // Minimal Incline the board support (in degrees) 
    private short _maxGrade = 0;                // Minimal Incline the board support (in degrees)  
    private short _currentGrade = 0;
    private KEYS _lastKeyCode = 0;              // Last hardware key that was pressed
    private string _consoleSerialNumber = "";   // Console Serial Number (On my board it is '450836-SS84F300346' ) 

    //OTHER variables that are not really related to the board----------------------------------------------------------
    private bool _keepTryingSiently = false;    // Indicate that we should wait for a HID Device connection silently (no logs or UI).
    private string _lastErrorStatus = "";       // Stores the last error (so we can show them on the UI).
    private string _RegisterInfo = "";          // Used just to log the last register requested or received during READ_WRITE commands

    public STATUS Status()
    {
        return _status;
    }

    public MODE Mode()
    {
        return _mode;
    }

    private void ChangeBoardStatus(STATUS newStatus)
    {
        if (_status != newStatus)
        {
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "ChangeBoardStatus", $"Request to change board status from '{_status}' to '{newStatus}'.");
            _status = newStatus;
        }
    }

    private void ChangeWorkoutMode(MODE newMode)
    {
        if (_mode != newMode)
        {
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "ChangeWorkoutMode", $"Request to change workout mode from '{_mode}' to '{newMode}'.");

            _mode = newMode;

            /*
            switch (_mode)
            {
                case MODE.UNKNOWN:         //  0,   //When the app started, indicates some check needs to be done to see the belt status
                    break;
                case MODE.IDLE:            //  1,   //Machine waits for user's input
                    break;
                case MODE.RUNNING:         //  2,   //Belt is moving
                    break;
                case MODE.PAUSE:           //  3,   //Belt is stopped, workout is still running
                    break;
                case MODE.RESULTS:         //  4,   //
                    break;
                case MODE.DEBUG:           //  5,
                    break;
                case MODE.LOG:             //  6,
                    break;
                case MODE.MAINTENANCE:     //  7,
                    break;
                case MODE.DMK:             //  8,   //SAFETY KEY OUT
                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, "ChangeWorkoutMode", $"SAFETY KEY WAS REMOVED! ");

                    break;
                case MODE.DEMO:            //  9,
                    break;
                case MODE.WARM_UP:         // 10,   //Board is going from 0 (actually 1.6km/h) to desired speed in 10 seconds
                    break;
                case MODE.COOL_DOWN:       // 11,   //Board is going from actual speed to 0 in 10 secods
                    break;
                case MODE.SLEEP:           // 12,
                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, "ChangeWorkoutMode", $"Waking up board... ");
                    break;
                case MODE.RESUME:          // 13,    //Resume belt movement and workout
                    break;
                case MODE.LOCKED:          // 14,
                    break;
                case MODE.PAUSE_OVERRRIDE: // 15
                    break;
            }
            */
        }
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
                    ChangeBoardStatus(STATUS.DISCONNECTED);
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

            _hidStream = _device?.Open()!;
            if (_hidStream == null)
            {
                if (!_keepTryingSiently)
                {
                    Helper.Log(Helper.LEVELS.ERROR, moduleName, "Open", "   Device could not be open. Is it opened by another process?");
                    ChangeBoardStatus(STATUS.DISCONNECTED);
                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Open", "      Will keep trying to open the device silently...");
                }

                _keepTryingSiently = true;
                return;
            }

            _hidStream.ReadTimeout = READ_TIMEOUT;
            _hidStream.WriteTimeout = WRITE_TIMEOUT;
            _outReportSize = _device!.GetMaxOutputReportLength();
            _inReportSize = _device!.GetMaxInputReportLength();

            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Open", $"   Device successfully open:");
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Open", $"      VendorID: {VID:X4}");
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Open", $"      ProductID: {PID:X4}");
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Open", $"      OutcomingReportSize: {_outReportSize}");
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Open", $"      IncomingReportSize: {_inReportSize}");
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Open", $"      Path: {_device.DevicePath}");

            ChangeBoardStatus(STATUS.HANDSHAKING);
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
            ChangeBoardStatus(STATUS.DISCONNECTING);

            _hidStream.ReadTimeout = 50;
            var buf = new byte[_inReportSize];
            for (int i = 0; i < 8; i++)
            {
                _hidStream.Read(buf, 0, buf.Length);
            }

            _hidStream.Close();
            ChangeBoardStatus(STATUS.DISCONNECTED);
        }
        catch (Exception e)
        {
            RaiseError($"Error while draining unread replies: '{e.Message.ToString()}'!");
        }

        try
        {
            _hidStream.Dispose();
        }
        catch (Exception e)
        {
            Helper.Log(Helper.LEVELS.ERROR, moduleName, "Dispose", "Error disposing HID stream: " + e.Message);
        }

        ChangeBoardStatus(STATUS.DISCONNECTED);
        _hidStream = null!;

    }

    public void Handshake()
    {
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Handshake", "Starting handshake protocol...");
        try
        {
            SendCommand(COMMAND.DEVICE_INFO);
            SendCommand(COMMAND.SUPPORTED_DEVICES);
            SendCommand(COMMAND.SUPPORTED_COMMANDS);
            SendCommand(COMMAND.SYSTEM_INFO);
            SendCommand(COMMAND.VERSION_INFO);
            SendCommand(COMMAND.SERIAL_NUMBER);
            SendCommand(COMMAND.VERIFY_SECURITY);

            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Handshake", "Handshake completed successfully.");

            ChangeBoardStatus(STATUS.CONNECTED);

        }
        catch (Exception e)
        {
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Handshake", "Error while executing handshake protocol:" + e.Message);
            ChangeBoardStatus(STATUS.DISCONNECTED);
        }
    }

    private byte[] ComputeVerifySecurity(uint serialNumber, uint partNumber, uint model, int masterLibraryVersion)
    {
        // Documented fix-up for one production run whose part number doesn't match its own hash.
        if (partNumber == 370357 && model == 39915) partNumber = 374677;

        static uint RotateLeft(uint v, int bits) => (v << bits) | (v >> (32 - bits));

        var hash = new byte[32];
        for (int b = 0; b < 32; b++)
        {
            hash[b] = (byte)((b + 1) & 0xFF);
            if (((serialNumber >> b) & 1) != 0)
            {
                uint p = b < 16 ? RotateLeft(partNumber, 16) >> b : partNumber >> b;
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

        return payload;

    }

    private string FormatToHex(byte[] frame, bool showAll = false, bool explain = false)
    {
        //'00 02 04 81 87 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF'
        string sBuffer = BitConverter.ToString(frame).Replace('-', ' ');
        string sResult = sBuffer;

        try
        {
            if (!showAll && frame.Length > 3)
            {
                sResult = sBuffer.Substring(0, ((frame[2] + 1) * 3)) + "...";
            }
        }
        catch
        {
        }

        sResult = "[" + sResult + "]";
        
        if (explain)
        {
            // reply: [04 07 02 02 00 00 0F]
            //         │  │  │  │  └──┴─────  ActualKph = 0.00 km/h (belt stopped)
            //         │  │  │  └──────────── Status
            //         │  │  └─────────────── ReadWriteData
            //         │  └────────────────── Length 7
            //         └───────────────────── Device TREADMILL

            sResult += "\n";
           
            //TODO:  Finish this algoritm
            for (int i = 1; i < sBuffer.Length -1 ; i++)
            {
                sResult += " |";
                sResult += "\n";
            }
        }
        return sResult;

    }

    private byte CheckSum(byte[] buffer, int from, int len)
    {
        int sum = 0;
        for (int i = from; i < len; i++)
        {
            sum += buffer[i];
        }

        return (byte)sum;
    }

    private byte GetDeviceId(COMMAND command)
    {
        byte deviceId = (byte)DEVICE.NONE;

        switch (command)
        {
            case COMMAND.DEVICE_INFO:
            case COMMAND.VERSION_INFO:
            case COMMAND.SYSTEM_INFO:
            case COMMAND.SERIAL_NUMBER:
                deviceId = (byte)DEVICE.MAIN; //0x00
                break;

            case COMMAND.SUPPORTED_DEVICES:
            case COMMAND.SUPPORTED_COMMANDS:
            case COMMAND.VERIFY_SECURITY:
            case COMMAND.READ_WRITE:
                deviceId = (byte)DEVICE.TREADMILL; //0x04
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
                Helper.Log(Helper.LEVELS.ERROR, moduleName, "createFrame", $"Failed to identify device for command '{command}'. Have you tried to list SUPPORTED_DEVICES?");
                deviceId = (byte)DEVICE.NONE;
                break;
        }

        return deviceId;
    }

    private void RaiseError(string msg)
    {
        _lastErrorStatus = msg;
        Helper.Log(Helper.LEVELS.ERROR, moduleName, "RaiseError", msg);
        ChangeBoardStatus(STATUS.ERROR);
        throw new IOException("_lastErrorStatus");
    }

    public string LastErrorMsg()
    {
        return _lastErrorStatus;
    }

    private byte[] Mask(IEnumerable<int> regs)
    {
        var list = regs.ToList();
        var mask = new byte[list.Max() / 8 + 1];
        foreach (var r in list) mask[r / 8] |= (byte)(1 << (r % 8));
        return mask;
    }

    public void StartBelt(Boolean warmUp = false)
    {
        if (warmUp)
        {
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"StartBelt", $"Starting belt warming up first is not implemented yet!");
        }
        else
        {
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"StartBelt", $"Starting belt...");
            SendCommand(COMMAND.READ_WRITE, REGISTER.WORKOUT_MODE, (int)MODE.RUNNING, true);
            Thread.Sleep(WRITE_TIMEOUT);
            SendCommand(COMMAND.READ_WRITE, REGISTER.KPH, (int) _minSpeed, true);
            SendCommand(COMMAND.READ_WRITE, REGISTER.GRADE, (int) _minGrade, true);
        }
    }

    public void StopBelt(Boolean coolDown = false)
    {
        if (coolDown)
        {
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"StopBelt", $"Stopping belt cooling down first is not implemented yet!");
        }
        else
        {
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"StopBelt", $"Stopping belt...");
            SendCommand(COMMAND.READ_WRITE, REGISTER.WORKOUT_MODE, (int)MODE.IDLE);
        }
    }
    
    private bool request(COMMAND command, byte[] payload = null!, bool logBuffer = true)
    {
        //-------------------------------------------------------------
        // REQUEST REPORT FORMAT:
        //-------------------------------------------------------------
        //      byte 0:      [REPORT ID]                                        
        //      byte 1:      [DEVICE ID]  - DEVICE Enum 
        //      byte 2:      [LENGTH]     - From DEVICE ID to CHECKSUM 
        //      byte 3:      [COMMAND]    - COMMAND Enum
        //      byte 4...n:  [PAYLOAD]    - 
        //      byte n + 1:  [CHECKSUM]   - Starts from byte 1, not 0
        //-------------------------------------------------------------
        //[00 02 06 82 00 00 8A FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.
        //[00 04 21 82 02 64 00 01 2B 37 00 00 84 D1 06 00 13 00 2B 7D 00 00 6C DC 02 64 00 00 00 01 02 00 00 37 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.
        //[00 02 06 00 00 FF 07 ...]'
        try
        {
            if (payload is null)
                payload = new byte[0];

            //Alocate space for the report
            byte[] report = new byte[_outReportSize];

            //Calculate the len of the frame. Notice that CheckSum starts on byte 1 [DEVICE ID] and not byte 0 [REPORT ID]
            byte len = (byte)(payload.Length + 4);

            //Make sure the payload fits. (If it is larger, truncate it)
            if (len > _outReportSize)
            {
                Helper.Log(Helper.LEVELS.WARN, moduleName, "request", $"      Payload was {len} bytes and would not fit on the report!");
                return false;
            }

            //Fill the frame with the "unused bytes" (0xFF)
            Array.Fill(report, (byte)0xFF);

            //Adds the REPORT_ID
            report[0] = REPORT_ID;

            //Add the deviceID (MainBoard, Treadmill, etc)
            report[1] = GetDeviceId(command);

            //Add the Len 
            report[2] = len;

            //Add the command 
            report[3] = (byte)command;

            //Copy the payload to the frame
            Array.Copy(payload, 0, report, 4, payload.Length);

            //Add CheckSum. Notice that CheckSum starts on byte 1 [DEVICE ID] and not byte 0 [REPORT ID]
            report[len] = CheckSum(report, 1, len);

            //Logs the whole report
            if (logBuffer)
                Helper.Log(Helper.LEVELS.DEBUG, moduleName, "request", $"   Sending....'{command}' command{_RegisterInfo}. Report is '{FormatToHex(report)}'.");

            //Sends the report
            _hidStream.Write(report);

            return true;
        }
        catch (Exception e)
        {
            Helper.Log(Helper.LEVELS.ERROR, moduleName, "request", $"      Failed to send '{command}' command: {e.Message.ToString()}.");
            return false;
        }
    }

    private byte[] receive(COMMAND command, bool logBuffer = true)
    {
        //-------------------------------------------------------------
        // RECEIVED REPORT FORMAT:
        //-------------------------------------------------------------
        //      byte 0:      REPORT ID (usually 0)                                      
        //      byte 1:      DEVICE ID (DEVICE Enum) 
        //      byte 2:      LENGTH (from byte 1 to byte n + 1)
        //      byte 3:      COMMAND (COMMAND Enum)
        //      byte 4:      STATUS
        //      byte 5...n:  PAYLOAD
        //      byte n + 1:  CHECKSUM
        //-------------------------------------------------------------

        try
        {
            byte[] payload = null!;
            int reportSize = 0;

            //Alocate space for the report
            byte[] report = new byte[_inReportSize];

            //Keeps reading data until there's nothing to read or we received the command answer
            do
            {
                //Reads the report from the HID stream
                reportSize = _hidStream.Read(report, 0, report.Length);

                //Validates the result
                if (reportSize <= _inReportSize)
                {
                    //Is this the answer we were waiting for?
                    var cmd = (COMMAND)report[3];
                    if (cmd == command)
                    {
                        //Is the length correct?
                        int len = report[2];
                        if (len > 5 && len < report.Length)
                        {
                            byte calculatedCheckSum = CheckSum(report, 1, len);
                            if (calculatedCheckSum == report[len])
                            {
                                //Check if the STATUS is OK
                                byte status = report[4];
                                if (status == STATUS_OK)
                                {
                                    //Log the whole report
                                    if (logBuffer)
                                        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "receive", $"   Received...'{command}' command{_RegisterInfo}. Report is '{FormatToHex(report)}'.");

                                    //Extrai o payload
                                    payload = new byte[len - 4];
                                    Array.Copy(report, 5, payload, 0, len - 4);

                                    //Retorna o payload apenas
                                    return payload;
                                }

                                if (status == STATUS_LOCKED) //TODO:  When is this supposed to happen? How to test it?
                                {
                                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, "receive", $"   Received...'{command}' command{_RegisterInfo}. Status is LOCKED for report '{FormatToHex(report)}'. Trying to answer challenge again.");
                                    SendCommand(COMMAND.VERIFY_SECURITY);
                                    return null!;
                                }

                                Helper.Log(Helper.LEVELS.WARN, moduleName, "receive", $"      Ignoring response for '{command}' command{_RegisterInfo}. Status is NOT OK ({status}) for report '{FormatToHex(report)}'!");

                            }
                            else
                                Helper.Log(Helper.LEVELS.WARN, moduleName, "receive", $"      Ignoring response for '{command}' command{_RegisterInfo}. CheckSum ({calculatedCheckSum} != {report[len]}) mismatch for report '{FormatToHex(report)}'!");
                        }
                        else
                            Helper.Log(Helper.LEVELS.WARN, moduleName, "receive", $"      Ignoring response for '{command}' command{_RegisterInfo}. ReportLen ({len}) mismatch for report '{FormatToHex(report)}'!");
                    }
                    else
                        Helper.Log(Helper.LEVELS.WARN, moduleName, "receive", $"      Ignoring response for '{command}' command{_RegisterInfo}. CommandID ({cmd}) mismatch for report '{FormatToHex(report)}'!");
                }
                else
                    Helper.Log(Helper.LEVELS.WARN, moduleName, "receive", $"      Ignoring response for '{command}' command{_RegisterInfo}. ReportSize ({reportSize}) mismatch for report '{FormatToHex(report)}'!");

            } while (reportSize > 0);

        }
        catch (Exception e)
        {
            Helper.Log(Helper.LEVELS.ERROR, moduleName, "receive", $"      Failed to read response for '{command}' command{_RegisterInfo}: {e.Message.ToString()}.");
        }

        return null!;
    }
    
    public byte[] SendCommand(COMMAND command, REGISTER? register = null, int? newValue = null, bool logBuffer = true)
    {
        byte[] payload = null!;
        byte[] answer = null!;
        _RegisterInfo = "";

        if (logBuffer)
            Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"Processing '{command}' command:");

        switch (command)
        {
            case COMMAND.DEVICE_INFO:
                //[00 02 04 81 87 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.
                //[00 04 1D 81 02 54 01 01 00 00 00 00 FF 0F DB FF FB DB FC FB FB 0C E7 1F 00 C0 D5 10 18 79 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]').

                if (request(command))
                {
                    answer = receive(command);
                    if (answer != null)
                    {

                        _softwareVersion = (answer.Length > 0 ? answer[0] : 0);
                        Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      Software Version: {_softwareVersion}."); // softwareVersion=84 

                        _softwareUnknownByte = (answer.Length > 2 ? BitConverter.ToUInt32(answer, 2) : 0);
                        Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      Software Unknown Byte: {_softwareUnknownByte}."); // serialNumber=1 

                        // TODO: There's probably more information we can find here....
                    }
                }

                break;

            case COMMAND.SUPPORTED_DEVICES:
                //[00 04 04 80 88 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.
                //[00 04 11 80 02 0B 4C 47 43 4D 4E 4A 40 46 4F 41 42 B5 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.

                if (request(command))
                {
                    answer = receive(command);
                    if (answer != null)
                    {
                        // TODO: There's probably more information we can find here....
                    }
                }

                break;

            case COMMAND.SUPPORTED_COMMANDS:
                //[00 04 04 88 90 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.
                //[00 04 0D 88 02 09 82 90 83 71 89 84 95 4C FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.

                if (request(command))
                {
                    answer = receive(command);
                    if (answer != null)
                    {
                        // TODO: There's probably more information we can find here.... 
                    }
                }

                break;

            case COMMAND.SYSTEM_INFO:
                //[00 02 06 82 00 00 8A FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.
                //[00 04 21 82 02 64 00 01 2B 37 00 00 84 D1 06 00 13 00 2B 7D 00 00 6C DC 02 64 00 00 00 01 02 00 00 37 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.

                payload = new byte[] { 0, 0 };
                if (request(command, payload))
                {
                    answer = receive(command);
                    if (answer != null)
                    {
                        _model = (answer.Length > 3 ? BitConverter.ToUInt32(answer, 3) : 0);
                        Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      Model: {_model}."); // Model=14123

                        _partNumber = (answer.Length > 7 ? BitConverter.ToUInt32(answer, 7) : 0);
                        Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      PartNumber: {_partNumber}."); // PartNumber=446852

                        // TODO: There's probably more information we can find here.... 
                    }
                }

                break;

            case COMMAND.VERSION_INFO:
                //[00 02 06 84 00 00 8C FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.
                //[00 04 1C 84 02 54 50 00 30 2E 31 2E 32 33 34 35 36 37 38 39 2E 30 31 32 33 01 2A 03 D5 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.

                payload = new byte[] { 0, 0 };
                if (request(command, payload))
                {
                    answer = receive(command);
                    if (answer != null)
                    {

                        _masterLibraryVersion = (answer.Length > 0 ? answer[0] : 0);
                        Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      MasterLibraryVersion: {_masterLibraryVersion}."); // MasterLibraryVersion=84 

                        // TODO: There's probably more information we can find here.... 
                    }
                }

                break;

            case COMMAND.SERIAL_NUMBER:
                //[00 02 04 95 9B FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.
                //[00 04 18 95 02 12 34 35 30 38 33 36 2D 53 53 38 34 46 33 30 30 33 34 36 B4 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.

                if (request(command))
                {
                    answer = receive(command);
                    if (answer != null)
                    {
                        _consoleSerialNumber = Encoding.ASCII.GetString(answer);
                        _consoleSerialNumber = _consoleSerialNumber.Substring(1, _consoleSerialNumber.Length - 2);
                        Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      Console Serial Number: {_consoleSerialNumber}.");  // _consoleSerialNumber=450836-SS84F300346
                    }
                }

                break;

            case COMMAND.VERIFY_SECURITY:
                //[00 04 28 90 07 5A 84 BC EE 26 50 98 C2 02 4C 84 C6 1E 58 B0 FA 2A 94 CC 3E 96 C0 28 92 F2 5C B4 16 6E C8 60 A0 02 00 00 FB FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.
                //[00 04 06 90 02 08 A4 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.
                if (_softwareVersion > 75)
                {
                    payload = ComputeVerifySecurity(_softwareUnknownByte, _partNumber, _model, _masterLibraryVersion);
                    if (request(command, payload))
                    {
                        //receives the answer
                        answer = receive(command);
                        if (answer != null)
                        {
                            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "SendCommand", $"      Board accepted the challenge (First byte of the payload is the UNLOCK key).");
                            
                            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "SendCommand", $"      Reading board's one time values"); 
                            
                            //BOARD LIMITES
                            SendCommand(COMMAND.READ_WRITE, REGISTER.MAX_KPH, null, false);     // 19.32 km/h
                            SendCommand(COMMAND.READ_WRITE, REGISTER.MIN_KPH, null, false);     // 1.60 km/h
                            SendCommand(COMMAND.READ_WRITE, REGISTER.MAX_GRADE, null, false);   // 12 degrees
                            SendCommand(COMMAND.READ_WRITE, REGISTER.MIN_GRADE, null, false);   // -3 degrees
                                
                            //BOARD USAGE
                            //SendCommand(COMMAND.READ_WRITE, REGISTER.SYSTEM_UNITS, null, false);          // 0 (Check if the converter is wrong. Buffer is actually '[00 0E])')    
                            //SendCommand(COMMAND.READ_WRITE, REGISTER.BELT_TOTAL_TIME, null, false);       // FAILS WITH LEN MISMATCH
                            //SendCommand(COMMAND.READ_WRITE, REGISTER.BELT_TOTAL_METERS, null, false);     // FAILS WITH LEN MISMATCH
                            //SendCommand(COMMAND.READ_WRITE, REGISTER.MOTOR_TOTAL_DISTANCE, null, false);  // 0 (Check if the converter is wrong. Buffer is actually '[00 00 00 00 11]')    
                            //SendCommand(COMMAND.READ_WRITE, REGISTER.TOTAL_TIME, null, false);            // 0 (Check if the converter is wrong. Buffer is actually '([00 00 00 00 11]')       

                            //USER INFO
                            //SendCommand(COMMAND.READ_WRITE, REGISTER.FIRST_NAME, null, false); // ':'   (Check if the converter is wrong. Buffer is actually '[00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 3A]')
                            //SendCommand(COMMAND.READ_WRITE, REGISTER.LAST_NAME, null, false);  // ':'   (Check if the converter is wrong. Buffer is actually '[00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 3A]')  
                            //SendCommand(COMMAND.READ_WRITE, REGISTER.USER_NAME, null, false);  // ':'   (Check if the converter is wrong. Buffer is actually '[00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 3A]')   
                            //SendCommand(COMMAND.READ_WRITE, REGISTER.HEIGHT, null, false);       // 170   (Check if the converter is wrong. Buffer is actually '[AA 00 B9]')      
                            //SendCommand(COMMAND.READ_WRITE, REGISTER.AGE, null, false);          // 35    (Check if the converter is wrong. Buffer is actually '[23 31]')
                            //SendCommand(COMMAND.READ_WRITE, REGISTER.WEIGHT, null, false);       // 83.91 (Check if the converter is wrong. Buffer is actually '[C7 20 F6]')
                            //SendCommand(COMMAND.READ_WRITE, REGISTER.GENDER, null, false);       // 1     (Check if the converter is wrong. Buffer is actually '[01 0F]')
                            
                            //TODO:  KEEP TRYING OTHER REGISTERS!
                            
                            
                            
                            //Finally says we are connected.
                            ChangeBoardStatus(STATUS.CONNECTED);  
                        }
                        else
                        {
                            Helper.Log(Helper.LEVELS.DEBUG, moduleName, "SendCommand", $"      Board DID NOT accept the challenge!");
                            RaiseError("Board did not accept the challenge!");
                            Dispose();
                        }
                    }
                }
                else
                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, "SendCommand", $"      Board (Software Version {_softwareVersion}) does not require the challenge!");

                break;

            case COMMAND.READ_WRITE:
                //[00 04 08 02 00 02 00 10 20 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.
                //[00 04 06 02 02 01 0F FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF]'.

                //Creates the payload
                var regs = new int[] { (int)register! };
                var mask = Mask(regs);
                payload = new byte[2 + mask.Length];
                payload[0] = 0x00;
                payload[1] = (byte)mask.Length;
                mask.CopyTo(payload, 2);

                _RegisterInfo = $" (Register: '{register}'{(newValue != null ? " Value: '"  + newValue.ToString() + "'" : "")})";

                if (request(command, payload, logBuffer))
                {
                    //receives the answer
                    answer = receive(command, logBuffer);
                    if (answer != null)
                    {
                        switch ((REGISTER) register)
                        {
                            case REGISTER.KPH:                      //   0   SpeedConverter           ReadOnly: false     - Target speed, hundredths (160 = 1.60 km/h). The ONLY usable speed value on this board since REG_ACTUAL_KPH returns always 0
                                _currentSpeed = BitConverter.ToUInt16(answer, 0);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      Current Speed: '{( (decimal) _currentSpeed / 100)}' km/h."); 
                                break;
                            
                            case REGISTER.GRADE:                    //   1   GradeConverter           ReadOnly: false     - Target incline, hundredths (1000 = 10.00 %)
                                _currentGrade = BitConverter.ToInt16(answer, 0);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      Current Grade: '{_currentGrade}' degrees."); 
                                break;

                            //case REGISTER.RESISTANCE:               //   2   ResistanceConverter      ReadOnly: false  
                            //case REGISTER.WATTS:                    //   3   ShortConverter           ReadOnly: true   
                            //case REGISTER.CURRENT_DISTANCE:         //   4   IntConverter             ReadOnly: true      - Advances while the belt turns — the reliable "is it moving" signal
                            //case REGISTER.RPM:                      //   5   ShortConverter           ReadOnly: true   
                            //case REGISTER.DISTANCE:                 //   6   IntConverter             ReadOnly: true   

                            case REGISTER.KEY_OBJECT: //   7   KeyObjConverter          ReadOnly: true      - Physical button press (code, raw code, timestamp, hold time)
                                var keyCode = (KEYS)(answer.Length > 0 ? answer[0] : 0);
                                if (keyCode != _lastKeyCode && keyCode != 0)
                                {
                                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"Hardware key '{keyCode} was pressed.");
                                    switch ((KEYS) keyCode)
                                    {
                                        case KEYS.START:
                                            switch ((MODE)_mode)
                                            {
                                                case MODE.IDLE:
                                                    StartBelt();
                                                    break;

                                                case MODE.WARM_UP:
                                                case MODE.COOL_DOWN:
                                                case MODE.RESUME:
                                                case MODE.RUNNING:
                                                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      WorkoutMode is '{_mode}', but belt is already moving. Nothing needs to be done!");
                                                    break;

                                                case MODE.DMK:
                                                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      SAFETY KEY IS NOT CONNECTED. INSERT IT AND TRY AGAIN!");
                                                    break;

                                                case MODE.PAUSE:
                                                case MODE.RESULTS:
                                                case MODE.SLEEP:
                                                case MODE.DEBUG:
                                                case MODE.LOG:
                                                case MODE.MAINTENANCE:
                                                case MODE.DEMO:
                                                case MODE.LOCKED:
                                                case MODE.PAUSE_OVERRRIDE:
                                                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      WorkoutMode is '{_mode}'. Setting it to IDLE first.");
                                                    SendCommand(COMMAND.READ_WRITE, REGISTER.WORKOUT_MODE, (int)MODE.IDLE, true);
                                                    StartBelt();
                                                    break;

                                                default:
                                                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      WORKOUTMODE IS '{_mode}' BUT NO ACTION WAS IMPLEMENTED FOR THIS STATUS YET!");
                                                    break;
                                            }
                                            break;

                                        case KEYS.STOP:
                                            switch ((MODE)_mode)
                                            {
                                                case MODE.WARM_UP:
                                                case MODE.COOL_DOWN:
                                                case MODE.RESUME:
                                                case MODE.RUNNING:
                                                    StopBelt(false);
                                                    break;

                                                case MODE.IDLE:
                                                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      WorkoutMode is already '{_mode}'. Nothing needs to be done!");
                                                    break;

                                                case MODE.DMK:
                                                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      SAFETY KEY IS NOT CONNECTED. INSERT IT AND TRY AGAIN!");
                                                    break;

                                                case MODE.PAUSE:
                                                case MODE.RESULTS:
                                                case MODE.DEBUG:
                                                case MODE.LOG:
                                                case MODE.MAINTENANCE:
                                                case MODE.DEMO:
                                                case MODE.SLEEP:
                                                case MODE.LOCKED:
                                                case MODE.PAUSE_OVERRRIDE:
                                                default:
                                                    Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      WORKOUTMODE IS '{_mode}' BUT NO ACTION WAS IMPLEMENTED FOR THIS STATUS YET!");
                                                    break;
                                            }

                                            break;

                                        case KEYS.SPEED_UP:
                                        case KEYS.SPEED_DOWN:
                                        case KEYS.INCLINE_UP:
                                        case KEYS.INCLINE_DOWN:
                                        case KEYS.FAN_UP:
                                        case KEYS.FAN_DOWN:
                                        case KEYS.VOL_UP:
                                        case KEYS.VOL_DOWN:
                                            Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      KEY '{keyCode}' NOT IMPLEMENTED YET!");
                                            break;

                                        case KEYS.GRADE_03_NEG:
                                        case KEYS.GRADE_02_NEG:
                                        case KEYS.GRADE_01_NEG:
                                        case KEYS.GRADE_00:
                                        case KEYS.GRADE_01:
                                        case KEYS.GRADE_02:
                                        case KEYS.GRADE_03:
                                        case KEYS.GRADE_04:
                                        case KEYS.GRADE_05:
                                        case KEYS.GRADE_06:
                                        case KEYS.GRADE_08:
                                        case KEYS.GRADE_10:
                                        case KEYS.GRADE_12:
                                            //This already works without any request but it would be something like:  SendCommand(COMMAND.READ_WRITE, REGISTER.GRADE, (int) VALUE, true );
                                            Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      Incline keys such as '{keyCode}' are processed directly from the board. Nothing needs to be done!");
                                            break;

                                        case KEYS.SPEED_01:
                                        case KEYS.SPEED_02:
                                        case KEYS.SPEED_03:
                                        case KEYS.SPEED_04:
                                        case KEYS.SPEED_05:
                                        case KEYS.SPEED_06:
                                        case KEYS.SPEED_07:
                                        case KEYS.SPEED_08:
                                        case KEYS.SPEED_09:
                                        case KEYS.SPEED_10:
                                        case KEYS.SPEED_11:
                                        case KEYS.SPEED_12:
                                            //This might work without any request but it would be something like:  SendCommand(COMMAND.READ_WRITE, REGISTER.KPH, (int) VALUE, true );
                                            Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      Speed keys such as '{keyCode}' are processed directly from the board. Nothing needs to be done!");
                                            break;

                                        default:
                                            Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"      UNKNOWN KEY '{keyCode}' as pressed!");
                                            break;
                                    }
                                }

                                _lastKeyCode = keyCode;
                                break;

                            //case REGISTER.FAN_SPEED:                //   8   ByteConverter            ReadOnly: false     - Target fan speed
                            //case REGISTER.VOLUME:                   //   9   ByteConverter            ReadOnly: false     
                            //case REGISTER.PULSE:                    //  10   PulseConverter           ReadOnly: false     - Heart Rate
                            //case REGISTER.RUNNING_TIME:             //  11   IntConverter             ReadOnly: true   

                            case REGISTER.WORKOUT_MODE: //  12   ModeConverter            ReadOnly: false     - the console state machine, 1 byte
                                ChangeWorkoutMode((MODE) answer[0]);
                                break;

                            //case REGISTER.CALORIES:                 //  13   CaloriesConverter        ReadOnly: true   
                            //case REGISTER.AUDIO_SOURCE:             //  14   AudioSourceConverter     ReadOnly: false  
                            //case REGISTER.LAP_TIME:                 //  15   ShortConverter           ReadOnly: true   
                            //case REGISTER.ACTUAL_KPH:               //  16   SpeedConverter           ReadOnly: true      - Actual Speed. ⚠️ not implemented on this board — always 0
                            //case REGISTER.ACTUAL_INCLINE:           //  17   GradeConverter           ReadOnly: true      - Actual incline (signed)
                            //case REGISTER.ACTUAL_RESISTANCE:        //  18   ResistanceConverter      ReadOnly: true   
                            //case REGISTER.ACTUAL_DISTANCE:          //  19   IntConverter             ReadOnly: true   
                            //case REGISTER.CURRENT_TIME:             //  20   IntConverter             ReadOnly: true   
                            //case REGISTER.CURRENT_CALORIES:         //  21   CaloriesConverter        ReadOnly: true   
                            //case REGISTER.GOAL_TIME:                //  22   IntConverter             ReadOnly: false  
                            //case REGISTER.INTERVAL_KPH:             //  23   IntervalDoubleConverter  ReadOnly: true  
                            
                            case REGISTER.AGE:                      //  24   ByteConverter            ReadOnly: false  
                                byte age = answer[0];
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         AGE: '{age}' years old ({FormatToHex(answer, true, false)})."); 
                                break;   
                            
                            case REGISTER.WEIGHT:                   //  25   DoubleConverter          ReadOnly: false  
                                int weight = BitConverter.ToInt16(answer, 0);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         WEIGHT: '{weight/ 100.0:F2}' kg ({FormatToHex(answer, true, false)})."); 
                                break;
                            
                            //case REGISTER.GEAR:                     //  26   GearConverter            ReadOnly: false      
     
                            case REGISTER.MAX_GRADE:                //  27   GradeConverter           ReadOnly: true       - Machine MAX INCLINE
                                _maxGrade = BitConverter.ToInt16(answer, 0);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         Max Incline Grade:: '{_maxGrade / 100.0:F2}' degrees."); 
                                break;
                            
                            case REGISTER.MIN_GRADE:                //  28   GradeConverter           ReadOnly: true       - Machine MIN INCLINE
                                _minGrade = BitConverter.ToInt16(answer, 0);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         Min Incline Grade: '{_minGrade / 100.0:F2}' degrees."); 
                                break;
                            
                            //case REGISTER.TRANS_MAX:                //  29   ShortConverter           ReadOnly: false      

                            case REGISTER.MAX_KPH:                  //  30   SpeedConverter           ReadOnly: true       - Machine MAX SPEED
                                _maxSpeed = BitConverter.ToUInt16(answer, 0);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         Max Speed: '{((decimal) _maxSpeed / 100):F2}' km/h."); 
                                break;
                            
                            case REGISTER.MIN_KPH:                  //  31   SpeedConverter           ReadOnly: true       - Machine MIN SPEED
                                _minSpeed = BitConverter.ToUInt16(answer, 0);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         Min Speed: '{((decimal) _minSpeed / 100):F2}' km/h."); 
                                break;

                            //case REGISTER.IDLE_TIMEOUT:             //  34   ShortConverter           ReadOnly: false      - Seconds of Pause. On this machine it reads 120s (Related to the SLEEP State) 
                            //case REGISTER.PAUSE_TIMEOUT:            //  35   ShortConverter           ReadOnly: false  
                            
                            case REGISTER.SYSTEM_UNITS:             //  36   BoolConverter            ReadOnly: false  
                                byte systemUnits = answer[0];
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         SYSTEM_UNITS: '{systemUnits}' ({FormatToHex(answer, true, false)})."); 
                                break;    
                            
                            case REGISTER.GENDER:                   //  37   BoolConverter            ReadOnly: false 
                                byte gender = answer[0];
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         GENDER: '{gender}' ({FormatToHex(answer, true, false)})."); 
                                break;  
                            
                            case REGISTER.FIRST_NAME:               //  38   StringConverter          ReadOnly: false 
                                string firstName = Encoding.ASCII.GetString(answer);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         FIRST_NAME: '{firstName}' ({FormatToHex(answer, true, false)})."); 
                                break;
                            
                            case REGISTER.LAST_NAME:                //  39   StringConverter          ReadOnly: false 
                                string lastName = Encoding.ASCII.GetString(answer);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         LAST_NAME: '{lastName}'({FormatToHex(answer, true, false)})."); 
                                break;
                            
                            case REGISTER.USER_NAME:                //  40   StringConverter          ReadOnly: false 
                                string userName = Encoding.ASCII.GetString(answer);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         USER_NAME: '{userName}' ({FormatToHex(answer, true, false)})."); 
                                break;
                            
                            case REGISTER.HEIGHT:                   //  41   ShortConverter           ReadOnly: false 
                                int height = BitConverter.ToInt16(answer, 0);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         HEIGHT: '{height}' cm ({FormatToHex(answer, true, false)})."); 
                                break;
                            
                            //case REGISTER.MAX_RESISTANCE_LEVEL:     //  42   ByteConverter            ReadOnly: true    
                            //case REGISTER.MAX_WEIGHT:               //  43   DoubleConverter          ReadOnly: true   
                            //case REGISTER.WARMUP_DISTANCE:          //  44   IntConverter             ReadOnly: true   
                            //case REGISTER.WARMUP_TIME:              //  45   ShortConverter           ReadOnly: true   
                            //case REGISTER.WARMUP_TIMEOUT:           //  46   ShortConverter           ReadOnly: false  
                            //case REGISTER.WARMUP_CALORIES:          //  47   CaloriesConverter        ReadOnly: true   
                            //case REGISTER.INTERVAL_GRADE:           //  48   DoubleConverter          ReadOnly: true   
                            //case REGISTER.MAX_PULSE:                //  49   ByteConverter            ReadOnly: true   
                            //case REGISTER.WT_MAX_KPH:               //  51   SpeedConverter           ReadOnly: true   
                            //case REGISTER.AVERAGE_GRADE:            //  52   GradeConverter           ReadOnly: true   
                            //case REGISTER.WT_MAX_GRADE:             //  53   GradeConverter           ReadOnly: true   
                            //case REGISTER.AVERAGE_WATTS:            //  54   ShortConverter           ReadOnly: true   
                            //case REGISTER.MAX_WATTS:                //  55   ShortConverter           ReadOnly: true   
                            //case REGISTER.AVERAGE_RPM:              //  56   ShortConverter           ReadOnly: true   
                            //case REGISTER.MAX_RPM:                  //  57   ShortConverter           ReadOnly: true   
                            //case REGISTER.KPH_GOAL:                 //  58   SpeedConverter           ReadOnly: false  
                            //case REGISTER.GRADE_GOAL:               //  59   GradeConverter           ReadOnly: false  
                            //case REGISTER.RESISTANCE_GOAL:          //  60   ResistanceConverter      ReadOnly: false  
                            //case REGISTER.WATT_GOAL:                //  61   ShortConverter           ReadOnly: false  
                            //case REGISTER.RPM_GOAL:                 //  63   ShortConverter           ReadOnly: false  
                            //case REGISTER.DISTANCE_GOAL:            //  64   IntConverter             ReadOnly: false  
                            //case REGISTER.PULSE_GOAL:               //  65   ByteConverter            ReadOnly: false  
                            //case REGISTER.START_UP_TIME:            //  66   IntConverter             ReadOnly: true   
                            
                            //Board answers with a LEN MISMATCH,
                            case REGISTER.BELT_TOTAL_TIME:
                                int beltTotalTime = BitConverter.ToInt16(answer, 0);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         BELT_TOTAL_TIME: '{beltTotalTime}' ({FormatToHex(answer, true, false)})."); 
                                break;
                            
                            //Board answers with a LEN MISMATCH
                            case REGISTER.BELT_TOTAL_METERS:        //  68   IntConverter             ReadOnly: true      - Lifetime Machine's Belt total distance
                                int beltTotalMeter = BitConverter.ToInt16(answer, 0);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         BELT_TOTAL_METERS: '{beltTotalMeter}' ({FormatToHex(answer, true, false)})."); 
                                break;
                            
                            case REGISTER.MOTOR_TOTAL_DISTANCE:     //  69   IntConverter             ReadOnly: true   
                                int motorTotalDistance = BitConverter.ToInt16(answer, 0);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         MOTOR_TOTAL_DISTANCE: '{motorTotalDistance}' ({FormatToHex(answer, true, false)})."); 
                                break;                               

                            case REGISTER.TOTAL_TIME:               //  70   IntConverter             ReadOnly: true
                                int totalTime = BitConverter.ToInt16(answer, 0);
                                Helper.Log(Helper.LEVELS.DEBUG, moduleName, $"SendCommand", $"         TOTAL_TIME: '{totalTime}' ({FormatToHex(answer, true, false)})."); 
                                break;
                            
                            //case REGISTER.COOL_DOWN_TIMEOUT:        //  71   ShortConverter           ReadOnly: false  
                            //case REGISTER.COOL_DOWN_TIME:           //  72   ShortConverter           ReadOnly: true   
                            //case REGISTER.COOL_DOWN_DISTANCE:       //  73   IntConverter             ReadOnly: true   
                            //case REGISTER.COOL_DOWN_CALORIES:       //  74   CaloriesConverter        ReadOnly: true   
                            //case REGISTER.VERTICAL_METER_NET:       //  75   VerticalMeterConverter   ReadOnly: true   
                            //case REGISTER.VERTICAL_METER_GAIN:      //  76   VerticalMeterConverter   ReadOnly: true   
                            //case REGISTER.REPS:                     //  77   ShortConverter           ReadOnly: true   
                            //case REGISTER.LEFT_REPS:                //  78   ShortConverter           ReadOnly: true   
                            //case REGISTER.RIGHT_REPS:               //  79   ShortConverter           ReadOnly: true   
                            //case REGISTER.REP_LENGTH:               //  80   ShortConverter           ReadOnly: true   
                            //case REGISTER.REP_LEFT_LENGTH:          //  81   ShortConverter           ReadOnly: true   
                            //case REGISTER.REP_RIGHT_LENGTH:         //  82   ShortConverter           ReadOnly: true   
                            //case REGISTER.BURN_RATE:                //  83   BurnRateConverter        ReadOnly: false  
                            //case REGISTER.AVG_BURN_RATE:            //  84   BurnRateConverter        ReadOnly: true   
                            //case REGISTER.MAX_BURN_RATE:            //  85   BurnRateConverter        ReadOnly: true   
                            //case REGISTER.INTERVAL_RPM:             //  86   IntervalIntConverter     ReadOnly: true   
                            //case REGISTER.INTERVAL_RESISTANCE:      //  87   IntervalIntConverter     ReadOnly: true   
                            //case REGISTER.GOAL_CALORIES:            //  94   CaloriesConverter        ReadOnly: false  
                            //case REGISTER.IDLE_MODE_LOCKOUT:        //  95   BoolConverter            ReadOnly: false    -- Reads 0 on this machine (Related to the SLEEP State) 
                            //case REGISTER.START_REQUESTED:          //  96   BoolConverter            ReadOnly: true   
                            //case REGISTER.FAN_STATE:                //  98   FanStateConverter        ReadOnly: false  
                            //case REGISTER.ACTIVATION_LOCK:          // 100   ByteConverter            ReadOnly: false  
                            //case REGISTER.PAUSED_TIME:              // 103   IntConverter             ReadOnly: true   
                            //case REGISTER.SLEEP_TIMER_STATE:        // 107   BoolConverter            ReadOnly: false    -- Not Supported on this machine (Related to the SLEEP State) 
                            //case REGISTER.REQUIRE_START_REQUESTED:  // 108   BoolConverter            ReadOnly: false  
                            //case REGISTER.STROKES:                  // 109   ShortConverter           ReadOnly: true   
                            //case REGISTER.STROKES_PER_MIN:          // 110   ByteConverter            ReadOnly: true   
                            //case REGISTER.FIVE_HUNDRED_SPLIT:       // 111   ShortConverter           ReadOnly: true   
                            //case REGISTER.AVG_FIVE_HUNDRED_SPLIT:   // 112   ShortConverter           ReadOnly: true   
                            //case REGISTER.IS_CLUB_UNIT:             // 115   BoolConverter            ReadOnly: true   
                            //case REGISTER.IS_READY_TO_DISCONNECT:   // 116   BoolConverter            ReadOnly: true   
                            //case REGISTER.IS_CONSTANT_WATTS_MODE:   // 119   BoolConverter            ReadOnly: false 
                            default:
                                Helper.Log(Helper.LEVELS.ERROR, moduleName, "SendCommand", $"      Unable to send a command '{command}' because register '{register}' is not implemented yet!");
                                break;
                        }
                    }
                }

                break;

            default:
                Helper.Log(Helper.LEVELS.ERROR, moduleName, "SendCommand", $"   COMMAND '{command}' NOT IMPLEMENTED!.");
                break;
        }

        return null!;
    }

    public void ReadAllRegisters()
    {
        //READ CURRENT WORKOUT MODE
        SendCommand(COMMAND.READ_WRITE, REGISTER.WORKOUT_MODE, null, false);

        //READ PHISICAL BUTTONS-----------------------------------------------------------------------------------------
        SendCommand(COMMAND.READ_WRITE, REGISTER.KEY_OBJECT, null, false);

        //WAKEUP BOARD--------------------------------------------------------------------------------------------------
        //SendCommand(COMMAND.READ_WRITE, REGISTER.WORKOUT_MODE, (int) MODE.IDLE);

        //PRIMING------------------------------------------------------------------------------------------------------- 
        //SendCommand(COMMAND.READ_WRITE, REGISTER.IDLE_MODE_LOCKOUT, 0);    
        //SendCommand(COMMAND.READ_WRITE, REGISTER.REQUIRE_START_REQUESTED, 1);
        //SendCommand(COMMAND.READ_WRITE, REGISTER.START_REQUESTED, 1);

        //SET USER INFO-------------------------------------------------------------------------------------------------
        //SendCommand(COMMAND.READ_WRITE, REGISTER.AGE, 50);     // in years
        //SendCommand(COMMAND.READ_WRITE, REGISTER.WEIGHT, 110); // in kg
        //SendCommand(COMMAND.READ_WRITE, REGISTER.HEIGHT, 180); // in cm

        //CURRENT VALUES------------------------------------------------------------------------------------------------
        //SendCommand(COMMAND.READ_WRITE, REGISTER.DISTANCE);            // Set Distance goal (?)
        //SendCommand(COMMAND.READ_WRITE, REGISTER.DISTANCE_GOAL);       // Distance goal (?)
        //SendCommand(COMMAND.READ_WRITE, REGISTER.ACTUAL_DISTANCE);     // Current Distance (?)
        //
        //SendCommand(COMMAND.READ_WRITE, REGISTER.KPH);                 // Set Speed goal (?)
        //SendCommand(COMMAND.READ_WRITE, REGISTER.KPH_GOAL);            // Speed goal (?) 
        //SendCommand(COMMAND.READ_WRITE, REGISTER.ACTUAL_KPH);          // Current Speed (?)
        //
        //SendCommand(COMMAND.READ_WRITE, REGISTER.GRADE);               // Set Incline goal (?)
        //SendCommand(COMMAND.READ_WRITE, REGISTER.GRADE_GOAL);          // Incline goal (?) 
        //SendCommand(COMMAND.READ_WRITE, REGISTER.ACTUAL_INCLINE);      // Current Incline (?)
        //
        //SendCommand(COMMAND.READ_WRITE, REGISTER.RESISTANCE);          // Set Resistance goal (?)
        //SendCommand(COMMAND.READ_WRITE, REGISTER.RESISTANCE_GOAL);     // Resistance goal (?)
        //
        //SendCommand(COMMAND.READ_WRITE, REGISTER.ACTUAL_RESISTANCE);   // Current Resistence (?)
        //SendCommand(COMMAND.READ_WRITE, REGISTER.CALORIES);            // Set Calories goal (?)
        //SendCommand(COMMAND.READ_WRITE, REGISTER.GOAL_CALORIES);       // Calories goal (?)

        //CHECK IF THE BELT IS MOVING-----------------------------------------------------------------------------------
        // TODO: The safest method to check if the belt is moving is measure distance twice (with a delay) and calculate
        //       the difference of both distances.
        //SendCommand(COMMAND.READ_WRITE, REGISTER.CURRENT_DISTANCE);

    }

}    