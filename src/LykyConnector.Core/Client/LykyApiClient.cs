using System.Net.Http.Json;
using System.Text.Json;
using LykyConnector.Core.Config;
using LykyConnector.Core.Sign;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace LykyConnector.Core.Client;

public interface ILykyApiClient
{
    Task<LykyResponse<T>> PostAsync<T>(
        string path,
        Dictionary<string, string?>? bizParams = null,
        CancellationToken ct = default);
}

public class LykyApiClient : ILykyApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ISignService _signService;
    private readonly LykyOptions _options;
    private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;
    private readonly JsonSerializerOptions _jsonOptions;

    public LykyApiClient(
        HttpClient httpClient,
        ISignService signService,
        IOptions<LykyOptions> options)
    {
        _httpClient = httpClient;
        _signService = signService;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "Content-Type", "application/x-www-form-urlencoded");
        _httpClient.Timeout = TimeSpan.FromSeconds(15);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(r =>
                        (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            })
            .Build();
    }

    public async Task<LykyResponse<T>> PostAsync<T>(
        string path,
        Dictionary<string, string?>? bizParams = null,
        CancellationToken ct = default)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = _signService.BuildSignature(
            _options.AppId, _options.AppSecret, timestamp);

        var formData = new Dictionary<string, string?>
        {
            { "app_id", _options.AppId },
            { "timestamp", timestamp.ToString() },
            { "signature", signature },
            { "version", "v1" }
        };

        if (bizParams != null)
        {
            foreach (var kvp in bizParams)
            {
                formData[kvp.Key] = kvp.Value;
            }
        }

        var response = await _resiliencePipeline.ExecuteAsync(
            async ct2 =>
            {
                var content = new FormUrlEncodedContent(
                    formData.Where(kvp => kvp.Value != null)
                            .ToDictionary(k => k.Key, k => k.Value!));

                var request = new HttpRequestMessage(HttpMethod.Post, path)
                {
                    Content = content
                };

                return await _httpClient.SendAsync(request, ct2);
            },
            ct);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LykyResponse<T>>(_jsonOptions, ct);

        return result ?? new LykyResponse<T> { Code = -1, Message = "响应解析失败" };
    }
}
