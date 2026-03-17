import { AuditLogItem, ServerTimedResponse } from './upgrade-types';

import { buildCompactIsoTimestamp } from './upgrade-download-utils';

function fileTs(): string {
  return buildCompactIsoTimestamp();
}

export function buildAuditExportFileName(args: { auditTraceId: string; auditActor: string }): string {
  const ts = fileTs();
  const trace = (args.auditTraceId ?? '').trim();
  const actor = (args.auditActor ?? '').trim();
  const suffix = trace ? `trace-${trace}` : (actor ? `actor-${actor}` : 'all');
  return `audit-export-${suffix}-${ts}.json`;
}

export function buildIncidentBundleFileName(args: { traceId: string; runId: string }): string {
  const ts = fileTs();
  const trace = (args.traceId ?? '').trim();
  const runId = (args.runId ?? '').trim();
  const suffix = trace ? `trace-${trace}` : (runId ? `run-${runId}` : 'unknown');
  return `incident-bundle-${suffix}-${ts}.json`;
}

export function buildDebugPackFileName(args: { traceId: string; runId: string }): string {
  const ts = fileTs();
  const trace = (args.traceId ?? '').trim();
  const runId = (args.runId ?? '').trim();
  const suffix = trace ? `trace-${trace}` : (runId ? `run-${runId}` : 'unknown');
  return `debug-pack-${suffix}-${ts}.json`;
}

export function buildTicketMarkdownFileName(args: { runIdShort: string; ts?: string }): string {
  const ts = (args.ts ?? '').trim() || fileTs();
  const runIdShort = (args.runIdShort ?? '').trim() || 'unknown';
  return `ticket-upgrade-${runIdShort}-${ts}.md`;
}

export function buildCurlSnippetsFileName(args: { runIdShort: string; ts?: string }): string {
  const ts = (args.ts ?? '').trim() || fileTs();
  const runIdShort = (args.runIdShort ?? '').trim() || 'unknown';
  return `curl-upgrade-${runIdShort}-${ts}.txt`;
}

export function buildAuditExportText(args: {
  res: ServerTimedResponse<AuditLogItem>;
  max: number;
  actor: string;
  actionContains: string;
  traceId: string;
  sinceUtc: string;
}): { text: string; totalCount: number | null; returnedCount: number } {
  const items = args.res.items ?? [];
  const totalCount = Number.isFinite(args.res?.totalCount as any) ? (args.res?.totalCount ?? 0) : null;
  const returnedCount = items.length;
  const t = JSON.stringify({
    max: args.max || 5000,
    actor: (args.actor ?? '').trim() || null,
    actionContains: (args.actionContains ?? '').trim() || null,
    traceId: (args.traceId ?? '').trim() || null,
    sinceUtc: (args.sinceUtc ?? '').trim() || null,
    totalCount,
    returnedCount,
    items,
  }, null, 2);

  return { text: t, totalCount, returnedCount };
}
