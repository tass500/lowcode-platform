using System.Reflection;
using LowCodePlatform.Backend.Filters;
using LowCodePlatform.Backend.Swagger;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class ApiDeprecatedOperationFilterTests
{
    private sealed class MethodMarked
    {
        [ApiDeprecated(SunsetUtcIso = "2030-01-01T00:00:00Z")]
        public void Action() { }
    }

    [ApiDeprecated(SunsetUtcIso = "2031-06-15T12:00:00Z")]
    private sealed class ClassMarked
    {
        public void Action() { }
    }

    [Fact]
    public void ResolveAttribute_prefers_method_over_class()
    {
        var method = typeof(MethodMarked).GetMethod(nameof(MethodMarked.Action), BindingFlags.Instance | BindingFlags.Public)!;
        var attr = ApiDeprecatedOperationFilter.ResolveAttribute(method);
        Assert.NotNull(attr);
        Assert.Equal("2030-01-01T00:00:00Z", attr!.SunsetUtcIso);
    }

    [Fact]
    public void ResolveAttribute_falls_back_to_declaring_type()
    {
        var method = typeof(ClassMarked).GetMethod(nameof(ClassMarked.Action), BindingFlags.Instance | BindingFlags.Public)!;
        var attr = ApiDeprecatedOperationFilter.ResolveAttribute(method);
        Assert.NotNull(attr);
        Assert.Equal("2031-06-15T12:00:00Z", attr!.SunsetUtcIso);
    }

    [Fact]
    public void TryFormatSunset_invalid_returns_false()
    {
        Assert.False(ApiDeprecatedOperationFilter.TryFormatSunset("bad", out _, out _));
    }

    [Fact]
    public void ApplyDeprecationToOperation_sets_flags_and_extension()
    {
        var method = typeof(MethodMarked).GetMethod(nameof(MethodMarked.Action), BindingFlags.Instance | BindingFlags.Public)!;
        var op = new OpenApiOperation();
        ApiDeprecatedOperationFilter.ApplyDeprecationToOperation(op, method);

        Assert.True(op.Deprecated);
        Assert.True(op.Extensions.TryGetValue("x-sunset", out var ext));
        var s = Assert.IsType<OpenApiString>(ext);
        Assert.Contains("2030", s.Value, StringComparison.Ordinal);
        Assert.Contains("Sunset", op.Description, StringComparison.Ordinal);
    }
}
