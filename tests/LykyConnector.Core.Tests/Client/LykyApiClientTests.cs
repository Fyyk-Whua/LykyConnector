using System.Net;
using LykyConnector.Core.Client;
using LykyConnector.Core.Config;
using LykyConnector.Core.Sign;
using Microsoft.Extensions.Options;

namespace LykyConnector.Core.Tests.Client;

public class LykyApiClientTests
{
    [Fact]
    public async Task PostAsync_IncludesAllSystemParams()
    {
        var signService = new SignService();
        var options = Options.Create(new LykyOptions
        {
            AppId = "app123",
            AppSecret = "secret456",
            BaseUrl = "https://store-api.lyky.cn/"
        });

        var mockHandler = new MockHttpHandler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("application/x-www-form-urlencoded",
                request.Content?.Headers.ContentType?.MediaType);

            var body = request.Content!.ReadAsStringAsync().Result;
            Assert.Contains("app_id=app123", body);
            Assert.Contains("version=v1", body);
            Assert.Contains("signature=", body);
            Assert.Contains("timestamp=", body);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"name":"success","message":"ok","code":0,"status":200}""")
            };
        });

        var client = new LykyApiClient(mockHandler.CreateClient(), signService, options);
        var response = await client.PostAsync<object>("/test");

        Assert.True(response.IsSuccess);
        Assert.Equal(0, response.Code);
    }

    [Fact]
    public async Task PostAsync_IncludesBizParams()
    {
        var signService = new SignService();
        var options = Options.Create(new LykyOptions
        {
            AppId = "app123",
            AppSecret = "secret456"
        });

        var mockHandler = new MockHttpHandler(request =>
        {
            var body = request.Content!.ReadAsStringAsync().Result;
            Assert.Contains("sku=SKU001", body);
            Assert.Contains("stock=100", body);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"name":"success","message":"ok","code":0,"status":200,"data":[]}""")
            };
        });

        var client = new LykyApiClient(mockHandler.CreateClient(), signService, options);
        var bizParams = new Dictionary<string, string?>
        {
            { "sku", "SKU001" },
            { "stock", "100" }
        };

        var response = await client.PostAsync<List<SyncFailItem>>(
            "v1/product-sync/update-stock-one", bizParams);

        Assert.True(response.IsSuccess);
    }

    [Fact]
    public async Task PostAsync_RetryOn500()
    {
        var signService = new SignService();
        var options = Options.Create(new LykyOptions
        {
            AppId = "app123",
            AppSecret = "secret456"
        });

        var callCount = 0;
        var mockHandler = new MockHttpHandler(_ =>
        {
            callCount++;
            if (callCount < 3)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"name":"success","message":"ok","code":0,"status":200}""")
            };
        });

        var client = new LykyApiClient(mockHandler.CreateClient(), signService, options);
        var response = await client.PostAsync<object>("/test");

        Assert.True(response.IsSuccess);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task PostAsync_DoesNotRetryOn400()
    {
        var signService = new SignService();
        var options = Options.Create(new LykyOptions
        {
            AppId = "app123",
            AppSecret = "secret456"
        });

        var callCount = 0;
        var mockHandler = new MockHttpHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        });

        var client = new LykyApiClient(mockHandler.CreateClient(), signService, options);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.PostAsync<object>("/test"));

        Assert.Equal(1, callCount);
    }
}

/// <summary>
/// Simple mock HTTP handler for unit testing.
/// </summary>
public class MockHttpHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient()
    {
        return new HttpClient(this) { BaseAddress = new Uri("https://store-api.lyky.cn/") };
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request));
    }
}
