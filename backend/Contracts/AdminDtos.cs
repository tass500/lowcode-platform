namespace LowCodePlatform.Backend.Contracts;

public sealed record InstallationStatusResponse(
    DateTime ServerTimeUtc,
    Guid InstallationId,
    string CurrentVersion,
    string SupportedVersion,
    DateTime? ReleaseDateUtc,
    int? UpgradeWindowDays,
    string EnforcementState,
    int DaysOutOfSupport);

public sealed record UpgradeRunSummaryDto(
    Guid UpgradeRunId,
    string TargetVersion,
    string State,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    string TraceId);

public sealed record RecentUpgradeRunsResponse(DateTime ServerTimeUtc, List<UpgradeRunSummaryDto> Items);

public sealed record QueueUpgradeRunDto(
    Guid UpgradeRunId,
    string TargetVersion,
    string State,
    DateTime? StartedAtUtc,
    string TraceId);

public sealed record QueueUpgradeRunsResponse(DateTime ServerTimeUtc, List<QueueUpgradeRunDto> Items);

public sealed record UpgradeRunStepDto(
    string StepKey,
    string State,
    int Attempt,
    DateTime? NextRetryAtUtc,
    string? LastErrorCode,
    string? LastErrorMessage,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc);

public sealed record UpgradeRunDetailsResponse(
    DateTime ServerTimeUtc,
    Guid UpgradeRunId,
    Guid InstallationId,
    string TargetVersion,
    string State,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    string? ErrorCode,
    string? ErrorMessage,
    string TraceId,
    IEnumerable<UpgradeRunStepDto> Steps);

public sealed record SimpleStatusResponse(DateTime ServerTimeUtc, string Status);

public sealed record StartUpgradeResponse(DateTime ServerTimeUtc, Guid UpgradeRunId);

public sealed record ObservabilityActiveRunDto(
    Guid UpgradeRunId,
    string TargetVersion,
    string State,
    DateTime? StartedAtUtc,
    string TraceId);

public sealed record ObservabilityLastAuditDto(
    Guid AuditLogId,
    DateTime TimestampUtc,
    string Actor,
    string Action,
    string Target,
    string TraceId);

public sealed record ObservabilityResponse(
    DateTime ServerTimeUtc,
    Guid InstallationId,
    string EnforcementState,
    int DaysOutOfSupport,
    List<ObservabilityActiveRunDto> ActiveRuns,
    ObservabilityLastAuditDto? LastAudit);

public sealed record AuditLogItemDto(
    Guid AuditLogId,
    DateTime TimestampUtc,
    string Actor,
    string Action,
    string Target,
    string TraceId,
    string? DetailsJson);

public sealed record AuditListResponse(DateTime ServerTimeUtc, List<AuditLogItemDto> Items, int TotalCount);
