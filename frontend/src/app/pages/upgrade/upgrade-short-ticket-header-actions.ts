import {
  InstallationStatus,
  ObservabilitySnapshot,
  UpgradeRun,
} from './upgrade-types';

export function shortTicketHeaderTextImpl(args: {
  runId: string;
  run: UpgradeRun | null;
  status: InstallationStatus | null;
  observability: ObservabilitySnapshot | null;
  shortId: (v: string) => string;
}): string {
  const installationId = (args.status?.installationId ?? args.observability?.installationId ?? '').trim();
  const enforcementState = (args.status?.enforcementState ?? args.observability?.enforcementState ?? '').trim();
  const id = (args.runId ?? '').trim();
  const st = (args.run?.state ?? '').trim();
  const tgt = (args.run?.targetVersion ?? '').trim();
  const trace = (args.run?.traceId ?? '').trim();

  const parts: string[] = [];
  if (installationId) parts.push(`inst=${args.shortId(installationId)}`);
  if (enforcementState) parts.push(`enf=${enforcementState}`);
  if (id) parts.push(`run=${args.shortId(id)}`);
  if (st) parts.push(`st=${st}`);
  if (tgt) parts.push(`tgt=${tgt}`);
  if (trace) parts.push(`trace=${args.shortId(trace)}`);
  return parts.join(' | ');
}

export function copyShortTicketHeaderWiringImpl(args: {
  run: UpgradeRun | null;
  buildText: () => string;
  copyText: (text: string) => Promise<void>;
}): void {
  if (!args.run) return;
  const t = args.buildText();
  if (!t) return;
  void args.copyText(t);
}
