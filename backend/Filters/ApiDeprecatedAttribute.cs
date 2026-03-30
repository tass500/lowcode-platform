namespace LowCodePlatform.Backend.Filters;

/// <summary>
/// Marks a controller or action as deprecated. Response headers follow RFC 8594 patterns:
/// <c>Deprecation</c> (boolean) and optional <c>Sunset</c> (HTTP-date).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class ApiDeprecatedAttribute : Attribute
{
    /// <summary>
    /// Optional instant (ISO 8601, UTC recommended) used for the <c>Sunset</c> header (HTTP-date).
    /// Invalid or empty values omit <c>Sunset</c>.
    /// </summary>
    public string? SunsetUtcIso { get; init; }
}
