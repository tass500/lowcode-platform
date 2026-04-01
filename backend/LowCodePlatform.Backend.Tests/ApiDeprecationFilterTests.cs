using LowCodePlatform.Backend.Filters;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class ApiDeprecationFilterTests
{
    [Fact]
    public void ApplyDeprecationHeaders_sets_Deprecation_and_Sunset()
    {
        var http = new DefaultHttpContext();
        var attr = new ApiDeprecatedAttribute { SunsetUtcIso = "2030-01-01T00:00:00Z" };

        ApiDeprecationFilter.ApplyDeprecationHeaders(http, attr);

        Assert.Equal("true", http.Response.Headers["Deprecation"].ToString());
        Assert.Contains("2030", http.Response.Headers["Sunset"].ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyDeprecationHeaders_invalid_Sunset_omits_Sunset_header()
    {
        var http = new DefaultHttpContext();
        var attr = new ApiDeprecatedAttribute { SunsetUtcIso = "not-a-date" };

        ApiDeprecationFilter.ApplyDeprecationHeaders(http, attr);

        Assert.Equal("true", http.Response.Headers["Deprecation"].ToString());
        Assert.False(http.Response.Headers.ContainsKey("Sunset"));
    }

    [Fact]
    public void ApplyDeprecationHeaders_empty_Sunset_omits_Sunset_header()
    {
        var http = new DefaultHttpContext();
        var attr = new ApiDeprecatedAttribute();

        ApiDeprecationFilter.ApplyDeprecationHeaders(http, attr);

        Assert.Equal("true", http.Response.Headers["Deprecation"].ToString());
        Assert.False(http.Response.Headers.ContainsKey("Sunset"));
    }
}
