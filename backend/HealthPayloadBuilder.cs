using System.Reflection;

namespace LowCodePlatform.Backend;

/// <summary>
/// Shared JSON bodies for /health, /api/health, /health/live, /health/ready (iter 60b).
/// </summary>
internal static class HealthPayloadBuilder
{
    public const string ServiceName = "lowcode-platform-backend";

    public static object Live()
        => new
        {
            status = "ok",
            service = ServiceName,
            version = GetVersion(),
        };

    public static object Ready()
        => new
        {
            status = "ok",
            service = ServiceName,
            version = GetVersion(),
            managementDb = "ok",
        };

    private static string GetVersion()
    {
        var asm = Assembly.GetEntryAssembly() ?? typeof(HealthPayloadBuilder).Assembly;
        return asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? asm.GetName().Version?.ToString()
            ?? "unknown";
    }
}
