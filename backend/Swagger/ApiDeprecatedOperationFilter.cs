using System.Globalization;
using System.Reflection;
using LowCodePlatform.Backend.Filters;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LowCodePlatform.Backend.Swagger;

/// <summary>
/// Sets OpenAPI <see cref="OpenApiOperation.Deprecated"/> (and optional <c>x-sunset</c>) when the action carries <see cref="ApiDeprecatedAttribute"/>.
/// </summary>
public sealed class ApiDeprecatedOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ApplyDeprecationToOperation(operation, context.MethodInfo);
    }

    internal static void ApplyDeprecationToOperation(OpenApiOperation operation, MethodInfo? methodInfo)
    {
        var attr = ResolveAttribute(methodInfo);
        if (attr is null)
            return;

        operation.Deprecated = true;

        if (TryFormatSunset(attr.SunsetUtcIso, out var httpDate, out var iso8601Utc))
        {
            operation.Extensions["x-sunset"] = new OpenApiString(iso8601Utc);
            AppendDescription(operation, $"Deprecated. Sunset (HTTP-date): {httpDate}.");
        }
        else
            AppendDescription(operation, "Deprecated.");
    }

    internal static ApiDeprecatedAttribute? ResolveAttribute(MethodInfo? methodInfo)
    {
        if (methodInfo is null)
            return null;

        var onMethod = methodInfo.GetCustomAttribute<ApiDeprecatedAttribute>(inherit: false);
        if (onMethod is not null)
            return onMethod;

        return methodInfo.DeclaringType?.GetCustomAttribute<ApiDeprecatedAttribute>(inherit: true);
    }

    internal static bool TryFormatSunset(string? sunsetUtcIso, out string httpDateRfc1123, out string iso8601UtcZ)
    {
        httpDateRfc1123 = string.Empty;
        iso8601UtcZ = string.Empty;

        if (string.IsNullOrWhiteSpace(sunsetUtcIso))
            return false;

        if (!DateTimeOffset.TryParse(
                sunsetUtcIso,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dto))
            return false;

        var utc = dto.ToUniversalTime();
        httpDateRfc1123 = utc.ToString("R", CultureInfo.InvariantCulture);
        iso8601UtcZ = utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        return true;
    }

    private static void AppendDescription(OpenApiOperation operation, string sentence)
    {
        var suffix = sentence.Trim();
        if (string.IsNullOrWhiteSpace(operation.Description))
            operation.Description = suffix;
        else
            operation.Description = $"{operation.Description.TrimEnd()}\n\n{suffix}";
    }
}
