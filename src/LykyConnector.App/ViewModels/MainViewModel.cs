using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LykyConnector.App.ViewModels;

public class NavItem
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isRunning = true;

    [ObservableProperty]
    private string _storeInfo = "店铺 3 | 今日订单 128";

    [ObservableProperty]
    private NavItem? _selectedNav;

    [ObservableProperty]
    private string _statusNetwork = "正常";

    [ObservableProperty]
    private string _statusErp = "已连接";

    [ObservableProperty]
    private int _statusQueueCount;

    [ObservableProperty]
    private string _statusCpu = "3%";

    [ObservableProperty]
    private string _statusMemory = "210MB";

    [ObservableProperty]
    private ObservableCollection<NavItem> _navItems = new()
    {
        new NavItem { Name = "运行看板", Icon = "\uE009" },
        new NavItem { Name = "订单流水", Icon = "\uE12A" },
        new NavItem { Name = "同步管理", Icon = "\uE0B9" },
        new NavItem { Name = "日志中心", Icon = "\uE0A3" },
        new NavItem { Name = "告警中心", Icon = "\uE076" },
        new NavItem { Name = "系统配置", Icon = "\uE023" }
    };
}
