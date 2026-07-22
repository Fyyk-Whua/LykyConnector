using Serilog;

namespace LykyConnector.Watchdog;

public class Program
{
    public static void Main(string[] args)
    {
        Directory.CreateDirectory("logs");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("logs/watchdog-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .CreateLogger();

        Log.Information("看门狗守护进程启动");

        Log.CloseAndFlush();
    }
}
