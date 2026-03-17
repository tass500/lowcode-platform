export type InstallationStatus = {
  serverTimeUtc?: string;
  installationId: string;
  currentVersion: string;
  supportedVersion: string;
  releaseDateUtc?: string;
  upgradeWindowDays?: number;
  enforcementState: 'ok' | 'warn' | 'soft_block' | 'hard_block';
  daysOutOfSupport: number;
};

export type ServerTimedResponse<T> = {
  serverTimeUtc?: string;
  items: T[];
  totalCount?: number;
};

export type ServerTimedSingleResponse<T> = {
  serverTimeUtc?: string;
} & T;

export type StartUpgradeResponse = {
  serverTimeUtc?: string;
  upgradeRunId: string;
};

export type UpgradeRunStep = {
  stepKey: string;
  state: string;
  attempt: number;
  nextRetryAtUtc: string | null;
  lastErrorCode: string | null;
  lastErrorMessage: string | null;
  startedAtUtc: string | null;
  finishedAtUtc: string | null;
};

export type UpgradeRun = {
  upgradeRunId: string;
  installationId: string;
  targetVersion: string;
  state: string;
  startedAtUtc: string | null;
  finishedAtUtc: string | null;
  errorCode: string | null;
  errorMessage: string | null;
  traceId: string;
  steps: UpgradeRunStep[];
};

export type RecentUpgradeRun = {
  upgradeRunId: string;
  targetVersion: string;
  state: string;
  startedAtUtc: string | null;
  finishedAtUtc: string | null;
  traceId: string;
};

export type QueueUpgradeRun = {
  upgradeRunId: string;
  targetVersion: string;
  state: string;
  startedAtUtc: string | null;
  traceId: string;
};

export type AuditLogItem = {
  auditLogId: string;
  timestampUtc: string;
  actor: string;
  action: string;
  target: string;
  traceId: string;
  detailsJson: string | null;
};

export type ObservabilityActiveRun = {
  upgradeRunId: string;
  targetVersion: string;
  state: string;
  startedAtUtc: string | null;
  traceId: string;
};

export type ObservabilityLastAudit = {
  auditLogId: string;
  timestampUtc: string;
  actor: string;
  action: string;
  target: string;
  traceId: string;
};

export type ObservabilitySnapshot = {
  serverTimeUtc?: string;
  installationId: string;
  enforcementState: 'ok' | 'warn' | 'soft_block' | 'hard_block';
  daysOutOfSupport: number;
  activeRuns: ObservabilityActiveRun[];
  lastAudit: ObservabilityLastAudit | null;
};
