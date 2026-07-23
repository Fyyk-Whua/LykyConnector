using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace LykyConnector.App.Services;

public class TrayService : IDisposable
{
    private readonly TaskbarIcon _trayIcon;

    public TrayService()
    {
        _trayIcon = new TaskbarIcon
        {
            Icon = new System.Drawing.Icon("app.ico"),
            ToolTipText = "邻医云对接服务 · 运行中"
        };

        var menu = new System.Windows.Controls.ContextMenu();

        var showItem = new System.Windows.Controls.MenuItem { Header = "显示主界面" };
        showItem.Click += (_, _) => Application.Current.MainWindow?.Show();

        var aboutItem = new System.Windows.Controls.MenuItem { Header = "关于" };
        aboutItem.Click += (_, _) =>
        {
            System.Windows.MessageBox.Show(
                "邻医云 B2C 对接程序 v1.0\n\n" +
                "药店桌面常驻对接网关\n" +
                "技术栈：C# .NET 10 + WPF",
                "关于邻医云对接服务");
        };

        var exitItem = new System.Windows.Controls.MenuItem
        {
            Header = "退出程序（需管理员密码）",
            IsEnabled = false
        };

        menu.Items.Add(showItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(aboutItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenu = menu;
        _trayIcon.TrayMouseDoubleClick += (_, _) => Application.Current.MainWindow?.Show();
    }

    public void ShowBalloon(string title, string message, BalloonIcon icon = BalloonIcon.Info)
    {
        _trayIcon.ShowBalloonTip(title, message, icon);
    }

    public void Dispose()
    {
        _trayIcon.Dispose();
    }
}
