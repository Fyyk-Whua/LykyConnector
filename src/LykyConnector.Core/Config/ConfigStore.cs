using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace LykyConnector.Core.Config;

public interface IConfigStore
{
    AppConfig Load();
    void Save(AppConfig config);
}

public class ConfigStore : IConfigStore
{
    private readonly string _dbPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigStore(string basePath = "")
    {
        if (string.IsNullOrEmpty(basePath))
            basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appdata");

        Directory.CreateDirectory(basePath);
        _dbPath = Path.Combine(basePath, "lyky.db");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS config (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            )
            """;
        command.ExecuteNonQuery();
    }

    public AppConfig Load()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM config WHERE key = 'appconfig'";

        var result = command.ExecuteScalar() as string;
        if (string.IsNullOrEmpty(result))
            return new AppConfig();

        var config = JsonSerializer.Deserialize<AppConfig>(result, _jsonOptions) ?? new AppConfig();

        if (!string.IsNullOrEmpty(config.Lyky.AppSecret))
        {
            config.Lyky.AppSecret = DpapiProtector.Unprotect(config.Lyky.AppSecret);
        }

        return config;
    }

    public void Save(AppConfig config)
    {
        var clone = CloneConfig(config);

        if (!string.IsNullOrEmpty(clone.Lyky.AppSecret))
        {
            clone.Lyky.AppSecret = DpapiProtector.Protect(clone.Lyky.AppSecret);
        }

        var json = JsonSerializer.Serialize(clone, _jsonOptions);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR REPLACE INTO config (key, value) VALUES ('appconfig', @value)
            """;
        command.Parameters.AddWithValue("@value", json);
        command.ExecuteNonQuery();
    }

    private static AppConfig CloneConfig(AppConfig source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<AppConfig>(json)!;
    }
}
