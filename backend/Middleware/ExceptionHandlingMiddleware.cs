using LowCodePlatform.Backend.Contracts;

namespace LowCodePlatform.Backend.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            if (ctx.Response.HasStarted)
                throw;

            var traceId = TraceIdMiddleware.GetTraceId(ctx);
            _logger.LogError(ex, "Unhandled exception. TraceId={TraceId} Path={Path}", traceId, ctx.Request.Path.Value);

            ctx.Response.Clear();
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "application/json";

            var message = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";

            var res = new ErrorResponse(
                ErrorCode: "unhandled_exception",
                Message: message,
                TraceId: traceId,
                TimestampUtc: DateTime.UtcNow);

            await ctx.Response.WriteAsJsonAsync(res);
        }
    }
}
