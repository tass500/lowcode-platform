namespace LowCodePlatform.Backend.Middleware;

/// <summary>
/// Public HTTP API contract hints (version label on <c>/api/*</c> responses).
/// </summary>
public sealed class ApiLifecycleOptions
{
    public const string SectionName = "Api";

    /// <summary>
    /// Sent as <c>X-API-Version</c> on successful API responses under <c>/api</c>. Empty = header omitted.
    /// </summary>
    public string PublicVersion { get; set; } = "1";
}
