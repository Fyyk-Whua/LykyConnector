using System.Text.Json.Serialization;

namespace LykyConnector.Core.Client;

public class LykyResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonIgnore]
    public bool IsSuccess => Code == 0;
}

public class LykyResponse<T> : LykyResponse
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class SyncFailItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;

    [JsonPropertyName("num")]
    public int Num { get; set; }
}
