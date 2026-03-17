import {
  InstallationStatus,
  ObservabilitySnapshot,
  UpgradeRun,
} from './upgrade-types';

export function ticketHeaderTextUnifiedImpl(args: {
  runId: string;
  run: UpgradeRun | null;
  status: InstallationStatus | null;
  observability: ObservabilitySnapshot | null;
  clientTraceId: string;
  lastServerTimeUtcApplied: string;
  serverNowOffsetMs: number;
}): string {
  const id = (args.runId ?? '').trim();
  const st = (args.run?.state ?? '').trim();
  const tgt = (args.run?.targetVersion ?? '').trim();
  const trace = (args.run?.traceId ?? '').trim();
  const clientTraceId = (args.clientTraceId ?? '').trim();
  const serverTime = (args.lastServerTimeUtcApplied ?? '').trim();

  const installationId = (args.status?.installationId ?? args.observability?.installationId ?? '').trim();
  const enforcementState = (args.status?.enforcementState ?? args.observability?.enforcementState ?? '').trim();
  const daysOutOfSupportRaw = args.status?.daysOutOfSupport ?? args.observability?.daysOutOfSupport;
  const daysOutOfSupport = daysOutOfSupportRaw === 0 || !!daysOutOfSupportRaw ? String(daysOutOfSupportRaw) : '';

  const headerArg = clientTraceId ? `-H 'X-Trace-Id: ${clientTraceId}'` : '';

  const parts: string[] = [];
  parts.push('=== UPGRADE DEBUG HEADER ===');
  if (installationId) parts.push(`installationId: ${installationId}`);
  if (enforcementState) parts.push(`enforcement: ${enforcementState} (daysOutOfSupport=${daysOutOfSupport || '?'})`);
  if (id) parts.push(`runId: ${id}`);
  if (st) parts.push(`state: ${st}`);
  if (tgt) parts.push(`target: ${tgt}`);
  if (trace) parts.push(`traceId: ${trace}`);
  if (serverTime) parts.push(`serverTimeUtc: ${serverTime}`);
  if (serverTime) parts.push(`clientOffset: ${args.serverNowOffsetMs}ms`);
  if (headerArg) parts.push(`curlHeader: ${headerArg}`);
  return parts.join('\n');
}
