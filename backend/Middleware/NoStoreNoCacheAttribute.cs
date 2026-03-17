using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;

namespace LowCodePlatform.Backend.Middleware;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class NoStoreNoCacheAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var headers = context.HttpContext.Response.Headers;
        headers.CacheControl = "no-store, no-cache, must-revalidate";
        headers.Pragma = "no-cache";
        headers.Expires = "0";

        var asm = typeof(NoStoreNoCacheAttribute).Assembly;
        var version = asm.GetName().Version?.ToString() ?? "unknown";
        if (!headers.ContainsKey("X-LCP-Server-Version"))
            headers.Append("X-LCP-Server-Version", version);

        var env = context.HttpContext.RequestServices.GetService<IHostEnvironment>();
        if (env is not null && !headers.ContainsKey("X-LCP-Server-Environment"))
            headers.Append("X-LCP-Server-Environment", env.EnvironmentName ?? "unknown");

        var rev = Environment.GetEnvironmentVariable("SOURCE_VERSION")
                  ?? Environment.GetEnvironmentVariable("GIT_COMMIT")
                  ?? Environment.GetEnvironmentVariable("COMMIT_SHA");
        if (string.IsNullOrWhiteSpace(rev))
        {
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info))
                rev = info;
        }

        if (!string.IsNullOrWhiteSpace(rev) && !headers.ContainsKey("X-LCP-Server-Revision"))
            headers.Append("X-LCP-Server-Revision", rev);

        base.OnActionExecuting(context);
    }
}
