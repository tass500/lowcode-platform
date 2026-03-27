namespace LowCodePlatform.Backend.Contracts;

public sealed record ErrorDetail(string? Path, string Code, string Message, string Severity)
{
    public static List<ErrorDetail> Single(string? path, string code, string message, string severity = "error")
        => new List<ErrorDetail> { new ErrorDetail(path, code, message, severity) };
}

public sealed record ErrorResponse(
    string ErrorCode,
    string Message,
    string TraceId,
    DateTime TimestampUtc,
    List<ErrorDetail>? Details = null
);
