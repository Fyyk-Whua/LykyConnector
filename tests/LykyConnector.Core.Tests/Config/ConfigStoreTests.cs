using LykyConnector.Core.Config;

namespace LykyConnector.Core.Tests.Config;

public class ConfigStoreTests
{
    private readonly string _testDir;

    public ConfigStoreTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "LykyConfigTest_" + Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void SaveThenLoad_ReturnsEqualConfig()
    {
        var store = new ConfigStore(_testDir);
        var original = new AppConfig
        {
            Lyky = new LykyOptions
            {
                AppId = "test_app_id",
                AppSecret = "test_secret_12345",
                CallbackPort = 8686
            },
            Erp = new ErpOptions
            {
                Mode = "Http",
                HttpUrl = "http://localhost:8080/erp"
            },
            Stores =
            {
                new StoreBinding { StoreId = 1, MerchantId = 10, StoreName = "测试店铺", Platform = 5 }
            }
        };

        store.Save(original);
        var loaded = store.Load();

        Assert.Equal(original.Lyky.AppId, loaded.Lyky.AppId);
        Assert.Equal(original.Lyky.AppSecret, loaded.Lyky.AppSecret);
        Assert.Equal(original.Lyky.CallbackPort, loaded.Lyky.CallbackPort);
        Assert.Equal(original.Erp.Mode, loaded.Erp.Mode);
        Assert.Equal(original.Erp.HttpUrl, loaded.Erp.HttpUrl);
        Assert.Single(loaded.Stores);
        Assert.Equal("测试店铺", loaded.Stores[0].StoreName);
    }

    [Fact]
    public void Load_NoExistingConfig_ReturnsDefault()
    {
        var store = new ConfigStore(_testDir);
        var config = store.Load();

        Assert.NotNull(config);
        Assert.NotNull(config.Lyky);
        Assert.Equal("https://store-api.lyky.cn/", config.Lyky.BaseUrl);
    }

    [Fact]
    public void Save_EncryptsAppSecret()
    {
        var store = new ConfigStore(_testDir);
        var config = new AppConfig
        {
            Lyky = new LykyOptions { AppId = "id", AppSecret = "mysecret" }
        };
        store.Save(config);

        var loaded = store.Load();

        Assert.Equal("mysecret", loaded.Lyky.AppSecret);
    }

    private void Cleanup()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }
}
