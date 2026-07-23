using System.IO;
using System.Windows;
using LykyConnector.App.Services;
using LykyConnector.Core.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LykyConnector.App;

public partial class App : Application
{
    private IHost? _host;
    private TrayService? _tray;
    internal static bool IsExiting { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Directory.CreateDirectory("logs");
        Directory.CreateDirectory("appdata");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .CreateLogger();

        Log.Information("邻医云对接服务启动");

        _tray = new TrayService();

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();

        Task.Run(async () =>
        {
            try
            {
                _host = new HostBuilder()
                    .ConfigureServices((_, services) =>
                    {
                        services.AddLykyCore("appdata");
                    })
                    .UseSerilog()
                    .Build();

                await _host.StartAsync();
                Log.Information("后台服务已启动");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "后台服务启动失败");
            }
        });
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        IsExiting = true;
        _tray?.Dispose();
        if (_host != null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        Log.Information("邻医云对接服务已退出");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
