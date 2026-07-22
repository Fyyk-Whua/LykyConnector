using LykyConnector.Core.Sign;

namespace LykyConnector.Core.Tests.Sign;

public class SignServiceTests
{
    private readonly SignService _signService = new();

    [Fact]
    public void BuildSignature_DocumentExample_ReturnsExpectedValue()
    {
        var appId = "dcc8ce40b7f76e21fcbeefe63497f690f";
        var appSecret = "sdasklhdah2342jk4234h23kjsdas";
        var timestamp = 1634124321L;

        var signature = _signService.BuildSignature(appId, appSecret, timestamp);

        Assert.Equal("ZjczYTA2ODcxMTJkYTEyOGMwOTM3Y2Y3NDJiZWU0ZWI=", signature);
    }

    [Fact]
    public void VerifySignature_CorrectSignature_ReturnsTrue()
    {
        var appId = "dcc8ce40b7f76e21fcbeefe63497f690f";
        var appSecret = "sdasklhdah2342jk4234h23kjsdas";
        var timestamp = 1634124321L;
        var expectedSignature = "ZjczYTA2ODcxMTJkYTEyOGMwOTM3Y2Y3NDJiZWU0ZWI=";

        var result = _signService.VerifySignature(appId, appSecret, timestamp, "v1", expectedSignature);

        Assert.True(result);
    }

    [Fact]
    public void VerifySignature_WrongSignature_ReturnsFalse()
    {
        var appId = "dcc8ce40b7f76e21fcbeefe63497f690f";
        var appSecret = "sdasklhdah2342jk4234h23kjsdas";
        var timestamp = 1634124321L;

        var result = _signService.VerifySignature(appId, appSecret, timestamp, "v1", "wrongsignature");

        Assert.False(result);
    }

    [Fact]
    public void VerifySignature_EmptyString_ReturnsFalse()
    {
        var result = _signService.VerifySignature("a", "b", 1, "v1", "");

        Assert.False(result);
    }

    [Fact]
    public void BuildSignature_DifferentInputs_ProduceDifferentResults()
    {
        var sig1 = _signService.BuildSignature("app1", "secret1", 1000000000);
        var sig2 = _signService.BuildSignature("app2", "secret1", 1000000000);
        var sig3 = _signService.BuildSignature("app1", "secret2", 1000000000);
        var sig4 = _signService.BuildSignature("app1", "secret1", 2000000000);

        Assert.NotEqual(sig1, sig2);
        Assert.NotEqual(sig1, sig3);
        Assert.NotEqual(sig1, sig4);
    }
}
