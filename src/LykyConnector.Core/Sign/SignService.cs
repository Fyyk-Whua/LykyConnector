using System.Security.Cryptography;
using System.Text;

namespace LykyConnector.Core.Sign;

public class SignService : ISignService
{
    public string BuildSignature(string appId, string appSecret, long timestamp, string version = "v1")
    {
        var parameters = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "app_id", appId },
            { "app_secret", appSecret },
            { "timestamp", timestamp.ToString() },
            { "version", version }
        };

        var plain = new StringBuilder();
        foreach (var value in parameters.Values)
        {
            plain.Append(value);
        }

        var plainString = plain.ToString();
        var md5Bytes = MD5.HashData(Encoding.UTF8.GetBytes(plainString));
        var md5Hex = Convert.ToHexStringLower(md5Bytes);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(md5Hex));
    }

    public bool VerifySignature(string appId, string appSecret, long timestamp, string version, string expectedSignature)
    {
        if (string.IsNullOrEmpty(expectedSignature))
            return false;

        var computed = BuildSignature(appId, appSecret, timestamp, version);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(expectedSignature));
    }
}
