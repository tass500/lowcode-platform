namespace LowCodePlatform.Backend.Contracts;

public sealed record ErrorDetail(string? Path, string Code, string Message, string Severity);

public sealed record ErrorResponse(
    string ErrorCode,
    string Message,
    string TraceId,
    DateTime TimestampUtc,
    List<ErrorDetail>? Details = null
);
