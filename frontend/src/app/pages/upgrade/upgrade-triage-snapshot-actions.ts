import type { ServerBuildInfo } from './upgrade-server-build';

type TriageSnapshotAuditArgs = {
  take: number;
  skip: number;
  max: number;
  filters: {
    actor: string;
    actionContains: string;
    traceId: string;
    sinceUtc: string;
  };
};

export function triageSnapshotTextImpl(args: {
  statusDaysOutOfSupport: number | null | undefined;
  observabilityDaysOutOfSupport: number | null | undefined;

  runId: string;
  traceId: string;
  state: string;
  target: string;
  enforcementState: string;

  lastServerTimeUtcApplied: string;
  lastServerTimeUtcSource: string;

  ticketHeader: string;
  traceHeader: string;
  serverBuild: ServerBuildInfo | null;

  audit: TriageSnapshotAuditArgs;

  buildTriageSnapshotText: (a: {
    runId: string;
    traceId: string;
    state: string;
    target: string;
    enforcementState: string;
    daysOutOfSupport: string;
    lastServerTimeUtcApplied: string;
    lastServerTimeUtcSource: string;
    ticketHeader: string;
    traceHeader: string;
    serverBuild: ServerBuildInfo | null;
    audit: TriageSnapshotAuditArgs;
  }) => string;
}): string {
  const daysOutOfSupportRaw = args.statusDaysOutOfSupport ?? args.observabilityDaysOutOfSupport;
  const daysOutOfSupport = daysOutOfSupportRaw === 0 || !!daysOutOfSupportRaw ? String(daysOutOfSupportRaw) : '';

  return args.buildTriageSnapshotText({
    runId: (args.runId ?? '').trim(),
    traceId: (args.traceId ?? '').trim(),
    state: (args.state ?? '').trim(),
    target: (args.target ?? '').trim(),
    enforcementState: (args.enforcementState ?? '').trim(),
    daysOutOfSupport,
    lastServerTimeUtcApplied: args.lastServerTimeUtcApplied ?? '',
    lastServerTimeUtcSource: args.lastServerTimeUtcSource ?? '',
    ticketHeader: args.ticketHeader ?? '',
    traceHeader: args.traceHeader ?? '',
    serverBuild: args.serverBuild ?? null,
    audit: args.audit,
  });
}
