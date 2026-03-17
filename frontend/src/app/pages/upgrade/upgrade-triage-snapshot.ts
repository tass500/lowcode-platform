import { serverBuildHeaderLines, type ServerBuildInfo } from './upgrade-server-build';
import { toAbsoluteUrl, type AuditUrlFilters } from './upgrade-url-builders';
import { buildAuditUrls, normalizeAuditUrlFilters } from './upgrade-audit-urls';

export function buildTriageSnapshotText(args: {
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
  audit: {
    take: number;
    skip: number;
    max: number;
    filters: AuditUrlFilters;
  };
}): string {
  const runId = (args.runId ?? '').trim();
  const traceId = (args.traceId ?? '').trim();
  const state = (args.state ?? '').trim();
  const target = (args.target ?? '').trim();
  const enforcementState = (args.enforcementState ?? '').trim();
  const daysOutOfSupport = (args.daysOutOfSupport ?? '').trim();
  const ticketHeader = args.ticketHeader ?? '';
  const traceHeader = (args.traceHeader ?? '').trim();
  const serverBuild = args.serverBuild;

  const links: { label: string; url: string }[] = [];
  if (runId) links.push({ label: 'upgrade run', url: toAbsoluteUrl(`/api/admin/upgrade-runs/${encodeURIComponent(runId)}`) });
  links.push({ label: 'queue', url: toAbsoluteUrl('/api/admin/upgrade-runs/queue') });
  links.push({ label: 'installation status', url: toAbsoluteUrl('/api/admin/installation/status') });
  links.push({ label: 'observability', url: toAbsoluteUrl('/api/admin/observability') });

  const auditFilters = normalizeAuditUrlFilters({
    actor: args.audit.filters.actor,
    actionContains: args.audit.filters.actionContains,
    traceId: args.audit.filters.traceId,
    sinceUtc: args.audit.filters.sinceUtc,
  });

  const urls = buildAuditUrls({
    take: args.audit.take || 50,
    skip: args.audit.skip || 0,
    max: args.audit.max || 5000,
    filters: auditFilters,
  });

  links.push({ label: 'audit list', url: toAbsoluteUrl(urls.list) });
  links.push({ label: 'audit export', url: toAbsoluteUrl(urls.export) });

  if (traceId) {
    const urlsByTrace = buildAuditUrls({
      take: args.audit.take || 50,
      skip: 0,
      max: args.audit.max || 5000,
      filters: {
        ...auditFilters,
        traceId,
      },
    });

    links.push({ label: 'audit list (traceId)', url: toAbsoluteUrl(urlsByTrace.list) });
    links.push({ label: 'audit export (traceId)', url: toAbsoluteUrl(urlsByTrace.export) });
  }

  const lines: string[] = [];
  lines.push('=== UPGRADE TRIAGE SNAPSHOT ===');
  if (runId) lines.push(`runId: ${runId}`);
  if (state) lines.push(`state: ${state}`);
  if (target) lines.push(`target: ${target}`);
  if (traceId) lines.push(`traceId: ${traceId}`);
  if (enforcementState) lines.push(`enforcement: ${enforcementState} (daysOutOfSupport=${daysOutOfSupport || '?'})`);
  if (args.lastServerTimeUtcApplied) lines.push(`serverTimeUtc: ${args.lastServerTimeUtcApplied} (source=${args.lastServerTimeUtcSource || '?'})`);
  lines.push('');

  lines.push('--- Debug header ---');
  lines.push(ticketHeader);
  lines.push('');

  if (serverBuild) {
    lines.push('--- Server build ---');
    lines.push(...serverBuildHeaderLines(serverBuild));
    lines.push('');
  }

  if (traceHeader) {
    lines.push('--- Trace header ---');
    lines.push(traceHeader);
    lines.push('');
  }

  lines.push('--- Links ---');
  for (const l of links) {
    if (!l.url) continue;
    lines.push(`${l.label}: ${l.url}`);
  }
  lines.push('');

  return lines.join('\n');
}
