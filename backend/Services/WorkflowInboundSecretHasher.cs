using System.Security.Cryptography;
using System.Text;

namespace LowCodePlatform.Backend.Services;

/// <summary>Inbound webhook secrets are stored as SHA-256 hex (never persist plaintext).</summary>
public static class WorkflowInboundSecretHasher
{
    public const int MinSecretLength = 16;

    public static string Sha256HexUtf8(string secret)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(bytes);
    }

    /// <summary>Constant-time comparison of UTF-8 secret to stored hex digest.</summary>
    public static bool IsValidSecret(string providedSecret, string? storedSha256Hex)
    {
        if (string.IsNullOrEmpty(storedSha256Hex))
            return false;

        try
        {
            var providedHex = Sha256HexUtf8(providedSecret);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedHex),
                Encoding.UTF8.GetBytes(storedSha256Hex));
        }
        catch
        {
            return false;
        }
    }
}
