import type {
  AuditLogItem,
  InstallationStatus,
  ObservabilitySnapshot,
  QueueUpgradeRun,
  ServerTimedResponse,
  ServerTimedSingleResponse,
  UpgradeRun,
} from './upgrade-types';

import {
  buildIncidentBundleCurlSnippets,
  buildIncidentBundleOutput,
  buildIncidentBundlePayload,
} from './upgrade-incident-bundle';

export async function buildIncidentBundleImpl(args: {
  get: {
    auditTake: number;
    clientTraceId: string;
    runId: string;
  };
  time: {
    capturedAtUtc: string;
    timeDiagnostics: {
      simulatedClientDriftMinutes: number;
      lastServerTimeUtcApplied: string | null;
      lastServerTimeUtcSource: string | null;
      serverNowOffsetMs: number;
      clockDriftWarning: string | null;
    };
  };
  http: {
    fetchRun: (runId: string) => Promise<ServerTimedSingleResponse<UpgradeRun>>;
    fetchStatus: () => Promise<InstallationStatus>;
    fetchQueue: () => Promise<ServerTimedResponse<QueueUpgradeRun>>;
    fetchObservability: () => Promise<ObservabilitySnapshot>;
    fetchAudit: (auditUrl: string) => Promise<ServerTimedResponse<AuditLogItem>>;
  };
  build: {
    auditPanelSnapshot: () => any;
    headerTextUnified: (args: {
      runId: string;
      run: UpgradeRun | null;
      status: InstallationStatus;
      observability: ObservabilitySnapshot;
      clientTraceId: string;
    }) => string;
  };
}): Promise<{ payload: any; jsonText: string; header: string; text: string; fileName: string }> {
  const take = args.get.auditTake || 50;
  const clientTrace = (args.get.clientTraceId ?? '').trim();

  const runId = (args.get.runId ?? '').trim();
  const run = runId ? (await args.http.fetchRun(runId) as any as UpgradeRun) : null;
  const trace = (run?.traceId ?? '').trim();

  const curl = buildIncidentBundleCurlSnippets({
    take,
    clientTraceId: clientTrace,
    runId,
    traceId: trace,
  });

  const auditUrl = trace
    ? `/api/admin/audit?take=${encodeURIComponent(String(take))}&traceId=${encodeURIComponent(trace)}`
    : `/api/admin/audit?take=${encodeURIComponent(String(take))}&actionContains=${encodeURIComponent('upgrade_')}`;

  const [status, queue, observability, audit] = await Promise.all([
    args.http.fetchStatus(),
    args.http.fetchQueue(),
    args.http.fetchObservability(),
    args.http.fetchAudit(auditUrl),
  ]);

  const auditPanelSnapshot = args.build.auditPanelSnapshot();

  const header = args.build.headerTextUnified({
    runId,
    run,
    status,
    observability,
    clientTraceId: clientTrace,
  });

  const payload = buildIncidentBundlePayload({
    take,
    clientTraceId: clientTrace,
    runId,
    traceId: trace,
    capturedAtUtc: args.time.capturedAtUtc,
    timeDiagnostics: args.time.timeDiagnostics,
    status,
    queue,
    observability,
    run,
    audit,
    auditPanelSnapshot,
    curl,
  });

  return buildIncidentBundleOutput({
    headerText: header,
    payload,
    traceId: trace,
    runId,
  });
}
