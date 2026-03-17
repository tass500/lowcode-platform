import {
  AuditLogItem,
  InstallationStatus,
  ObservabilitySnapshot,
  QueueUpgradeRun,
  ServerTimedResponse,
  UpgradeRun,
} from './upgrade-types';

import { buildIncidentBundleFileName } from './upgrade-export-builders';

export function buildIncidentBundleCurlSnippets(args: {
  take: number;
  clientTraceId: string;
  runId: string;
  traceId: string;
}): {
  status: string;
  queue: string;
  recent: string;
  latest: string;
  observability: string;
  runGet: (id: string) => string;
  runRetry: (id: string) => string;
  runCancel: (id: string) => string;
  auditUpgrade: string;
  auditTrace: (t: string) => string;
} {
  const take = args.take || 50;
  const clientTrace = (args.clientTraceId ?? '').trim();
  const headerArg = clientTrace ? ` -H 'X-Trace-Id: ${clientTrace}'` : '';

  return {
    status: `curl -sS http://localhost:5002/api/admin/installation/status${headerArg}`,
    queue: `curl -sS http://localhost:5002/api/admin/upgrade-runs/queue${headerArg}`,
    recent: `curl -sS "http://localhost:5002/api/admin/upgrade-runs/recent?take=10"${headerArg}`,
    latest: `curl -sS http://localhost:5002/api/admin/upgrade-runs/latest${headerArg}`,
    observability: `curl -sS http://localhost:5002/api/admin/observability${headerArg}`,
    runGet: (id: string) => `curl -sS http://localhost:5002/api/admin/upgrade-runs/${id}${headerArg}`,
    runRetry: (id: string) => `curl -sS -X POST http://localhost:5002/api/admin/upgrade-runs/${id}/retry -H 'Content-Type: application/json'${headerArg} -d '{}'`,
    runCancel: (id: string) => `curl -sS -X POST http://localhost:5002/api/admin/upgrade-runs/${id}/cancel -H 'Content-Type: application/json'${headerArg} -d '{}'`,
    auditUpgrade: `curl -sS "http://localhost:5002/api/admin/audit?take=${encodeURIComponent(String(take))}&actionContains=${encodeURIComponent('upgrade_')}"${headerArg}`,
    auditTrace: (t: string) => `curl -sS "http://localhost:5002/api/admin/audit?take=${encodeURIComponent(String(take))}&traceId=${encodeURIComponent(t)}"${headerArg}`,
  };
}

export function buildIncidentBundlePayload(args: {
  take: number;
  clientTraceId: string;
  runId: string;
  traceId: string;
  capturedAtUtc: string;
  timeDiagnostics: {
    simulatedClientDriftMinutes: number;
    lastServerTimeUtcApplied: string | null;
    lastServerTimeUtcSource: string | null;
    serverNowOffsetMs: number;
    clockDriftWarning: string | null;
  };
  status: InstallationStatus;
  queue: ServerTimedResponse<QueueUpgradeRun>;
  observability: ObservabilitySnapshot;
  run: UpgradeRun | null;
  audit: ServerTimedResponse<AuditLogItem>;
  auditPanelSnapshot: any;
  curl: ReturnType<typeof buildIncidentBundleCurlSnippets>;
}): any {
  const runId = (args.runId ?? '').trim();
  const trace = (args.traceId ?? '').trim();

  return {
    clientTraceId: (args.clientTraceId ?? '').trim() || null,
    selectedRunId: runId || null,
    selectedRunTraceId: trace || null,
    capturedAtUtc: args.capturedAtUtc,
    timeDiagnostics: args.timeDiagnostics,
    status: args.status,
    queue: args.queue,
    observability: args.observability,
    run: args.run,
    audit: args.audit,
    auditPanelSnapshot: args.auditPanelSnapshot,
    curlSnippets: {
      status: args.curl.status,
      queue: args.curl.queue,
      recent: args.curl.recent,
      latest: args.curl.latest,
      observability: args.curl.observability,
      run: runId
        ? {
            get: args.curl.runGet(runId),
            retry: args.curl.runRetry(runId),
            cancel: args.curl.runCancel(runId),
          }
        : null,
      audit: trace
        ? {
            byTraceId: args.curl.auditTrace(trace),
            byUpgradeAction: args.curl.auditUpgrade,
          }
        : {
            byUpgradeAction: args.curl.auditUpgrade,
          },
    },
  };
}

export function buildIncidentBundleOutput(args: {
  headerText: string;
  payload: any;
  traceId: string;
  runId: string;
}): { payload: any; jsonText: string; header: string; text: string; fileName: string } {
  const header = args.headerText ?? '';
  const payload = args.payload;

  const jsonText = JSON.stringify(payload, null, 2);
  const text = [
    header,
    '',
    '=== INCIDENT BUNDLE (paste into ticket) ===',
    jsonText,
    '',
  ].join('\n');

  return {
    payload,
    jsonText,
    header,
    text,
    fileName: buildIncidentBundleFileName({ traceId: args.traceId, runId: args.runId }),
  };
}
