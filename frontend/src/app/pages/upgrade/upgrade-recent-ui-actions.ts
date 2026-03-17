import type { RecentUpgradeRun } from './upgrade-types';

export function recentLabelImpl(args: {
  r: RecentUpgradeRun;
  runId: string;
  shortId: (id: string) => string;
  recentRunDurationMs: (r: RecentUpgradeRun) => number | null;
  formatDuration: (ms: number | null) => string;
}): string {
  const selected = (args.runId ?? '').trim() && (args.runId ?? '').trim() === (args.r.upgradeRunId ?? '').trim();
  const prefix = selected ? '▶ ' : '';
  const started = args.r.startedAtUtc ? new Date(args.r.startedAtUtc).toLocaleString() : '—';
  const finished = args.r.finishedAtUtc ? new Date(args.r.finishedAtUtc).toLocaleString() : '—';
  const dur = args.formatDuration(args.recentRunDurationMs(args.r));
  return `${prefix}${args.r.targetVersion} | ${args.r.state} | ${args.shortId(args.r.upgradeRunId)} | ${dur} | ${started} → ${finished}`;
}

export function selectedRunRowStyleImpl(args: {
  id: string;
  runId: string;
}): Record<string, string> {
  const a = (args.id ?? '').trim();
  const b = (args.runId ?? '').trim();
  if (!a || !b) return {};
  if (a !== b) return {};
  return {
    background: '#fff6d6',
  };
}
