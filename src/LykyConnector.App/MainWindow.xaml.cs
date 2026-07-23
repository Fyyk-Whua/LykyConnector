using System.ComponentModel;
using System.Windows;

namespace LykyConnector.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Title = "邻医云对接服务";
        Width = 1100;
        Height = 720;
        MinWidth = 900;
        MinHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        InitializeComponent();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (App.IsExiting)
        {
            e.Cancel = false;
            return;
        }

        e.Cancel = true;
        Hide();
    }
}
