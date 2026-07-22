namespace LykyConnector.Core.Sign;

public interface ISignService
{
    string BuildSignature(string appId, string appSecret, long timestamp, string version = "v1");

    bool VerifySignature(string appId, string appSecret, long timestamp, string version, string expectedSignature);
}
