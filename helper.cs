using System.Text;

public static class Helper
{
    private static string _filePath = "";
    private static StreamWriter _streamWriter = null!;
    private static readonly object _lock = new();
    private static LEVELS _level = LEVELS.NONE;
    
    public enum LEVELS: byte
    {
        NONE       = 0,
        ERROR      = 1,
        WARN       = 2,
        DEBUG      = 3
    }
    
    public static void Log(LEVELS level, string module, string routine, string? data)
    {
        lock (_lock)
        {
            if (_streamWriter == null) return;
            if (level > _level) return;

            try
            {
                var sLevel = level.ToString().PadRight(5);
                var sTimestamp = DateTime.Now.ToString("HH:mm:ss.fff").PadRight(12);
                var sModule = module.PadRight(20);
                var sRoutine = routine.PadRight(20);

                var msg = $"{sTimestamp} | {sLevel} | {sModule} | {sRoutine} | {data}";
                
                Console.WriteLine(msg);
                _streamWriter.WriteLine(msg);
                
            }
            catch{ }

        }
    }
    public static void Init()
    {
        _filePath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? ".", $"treadmill-{DateTime.Now:yyyyMMdd}.log");
        _streamWriter = new StreamWriter(_filePath, false, new UTF8Encoding(false)) { AutoFlush = true };
        _level = LEVELS.DEBUG;
    }
}