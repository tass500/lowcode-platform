import type { QueueUpgradeRun, UpgradeRun } from './upgrade-types';

export function isActiveRunStateImpl(stateRaw: string | null | undefined): boolean {
  const st = (stateRaw ?? '').toLowerCase().trim();
  return st === 'pending' || st === 'running';
}

export function hasAnyActiveRunImpl(queueRuns: QueueUpgradeRun[] | null | undefined): boolean {
  return (queueRuns ?? []).some(x => {
    const st = (x.state ?? '').toLowerCase();
    return st === 'pending' || st === 'running';
  });
}

export function hasOtherActiveRunImpl(args: {
  runId: string;
  queueRuns: QueueUpgradeRun[] | null | undefined;
}): boolean {
  const id = (args.runId ?? '').trim();
  return (args.queueRuns ?? []).some(x => {
    const st = (x.state ?? '').toLowerCase();
    if (st !== 'pending' && st !== 'running') return false;
    if (!id) return true;
    return x.upgradeRunId !== id;
  });
}

export function isCancelableImpl(run: UpgradeRun | null | undefined): boolean {
  const st = (run?.state ?? '').toLowerCase();
  return st === 'pending' || st === 'running';
}
