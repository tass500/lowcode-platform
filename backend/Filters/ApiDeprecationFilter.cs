using System.Globalization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LowCodePlatform.Backend.Filters;

/// <summary>
/// Adds <c>Deprecation</c> / <c>Sunset</c> headers when <see cref="ApiDeprecatedAttribute"/> is present on the endpoint.
/// </summary>
public sealed class ApiDeprecationFilter : IAsyncResourceFilter
{
    public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var attr = context.ActionDescriptor.EndpointMetadata.OfType<ApiDeprecatedAttribute>().FirstOrDefault();
        if (attr is null)
            return next();

        context.HttpContext.Response.OnStarting(() =>
        {
            ApplyDeprecationHeaders(context.HttpContext, attr);
            return Task.CompletedTask;
        });

        return next();
    }

    internal static void ApplyDeprecationHeaders(HttpContext http, ApiDeprecatedAttribute attr)
    {
        var headers = http.Response.Headers;
        if (!headers.ContainsKey("Deprecation"))
            headers["Deprecation"] = "true";

        if (string.IsNullOrWhiteSpace(attr.SunsetUtcIso))
            return;

        if (!DateTimeOffset.TryParse(
                attr.SunsetUtcIso,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dto))
            return;

        var rfc1123 = dto.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture);
        if (!headers.ContainsKey("Sunset"))
            headers["Sunset"] = rfc1123;
    }
}
