namespace WinNTC1750;

static class Program
{
    private static string moduleName = "program.cs";
    
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Helper.Init();
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Main", "***************** START ********************"); 

         Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Main", "Application Initialize...");
        ApplicationConfiguration.Initialize();
    
        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Main", "Setting an UnhandledException Handler... ");
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Helper.Log(Helper.LEVELS.ERROR, moduleName, "Main",  e.ExceptionObject.ToString());

        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Main", "Preparing GUI...");
        Application.Run(new frmMain());

        Helper.Log(Helper.LEVELS.DEBUG, moduleName, "Main", "****************** END *********************");
    }
    
}
