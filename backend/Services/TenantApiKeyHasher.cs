using System.Security.Cryptography;
using System.Text;

namespace LowCodePlatform.Backend.Services;

public static class TenantApiKeyHasher
{
    public static string HashToHex(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool SlowEquals(string apiKey, string? storedHex)
    {
        if (string.IsNullOrWhiteSpace(storedHex) || storedHex.Length != 64)
            return false;

        byte[] storedBytes;
        try
        {
            storedBytes = Convert.FromHexString(storedHex);
        }
        catch
        {
            return false;
        }

        if (storedBytes.Length != 32)
            return false;

        var computed = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return CryptographicOperations.FixedTimeEquals(computed, storedBytes);
    }

    public static string GenerateRandomApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
