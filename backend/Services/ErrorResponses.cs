using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;

namespace LowCodePlatform.Backend.Services;

public static class ErrorResponses
{
    public static IResult Problem(HttpContext ctx, int statusCode, string errorCode, string message, List<ErrorDetail>? details = null)
    {
        var res = new ErrorResponse(
            ErrorCode: errorCode,
            Message: message,
            TraceId: TraceIdMiddleware.GetTraceId(ctx),
            TimestampUtc: DateTime.UtcNow,
            Details: details);

        return Results.Json(res, statusCode: statusCode);
    }
}
