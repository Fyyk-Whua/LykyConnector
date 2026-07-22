using System.Text.Json.Serialization;

namespace LykyConnector.Core.Config;

public class LykyOptions
{
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://store-api.lyky.cn/";
    public int CallbackPort { get; set; } = 8686;
}

public class ErpOptions
{
    public string Mode { get; set; } = "Http";
    public string HttpUrl { get; set; } = string.Empty;
    public string HttpToken { get; set; } = string.Empty;
    public string DbConnectionString { get; set; } = string.Empty;
    public Dictionary<string, string> FieldMapping { get; set; } = new();
}

public class StoreBinding
{
    public long StoreId { get; set; }
    public long MerchantId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int Platform { get; set; }
}

public class RunOptions
{
    public bool AutoStart { get; set; } = true;
    public bool ProcessGuard { get; set; } = true;
    public int ReconnectIntervalSec { get; set; } = 30;
    public bool SandboxMode { get; set; }
    public bool MaintenanceMode { get; set; }
}

public class AlertOptions
{
    public bool Enabled { get; set; } = true;
    public string WebhookUrl { get; set; } = string.Empty;
    public string WebhookType { get; set; } = "wechat";
    public bool SoundEnabled { get; set; } = true;
    public string? QuietHoursStart { get; set; }
    public string? QuietHoursEnd { get; set; }
}

public class AppConfig
{
    public LykyOptions Lyky { get; set; } = new();
    public ErpOptions Erp { get; set; } = new();
    public List<StoreBinding> Stores { get; set; } = new();
    public RunOptions Run { get; set; } = new();
    public AlertOptions Alert { get; set; } = new();

    [JsonIgnore]
    public string? AdminPasswordHash { get; set; }
}
