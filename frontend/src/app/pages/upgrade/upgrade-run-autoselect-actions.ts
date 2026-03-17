export type ActiveRunCandidate = { upgradeRunId: string; state: string };

export function tryAutoSelectActiveRunImpl(args: {
  source: string;

  runId: () => string;
  setRunId: (v: string) => void;

  observabilityCandidates: () => ActiveRunCandidate[];
  queueCandidates: () => ActiveRunCandidate[];

  pickActiveRunIdFromCandidates: (candidates: ActiveRunCandidate[]) => string;

  setAutoSelectedActiveRunSource: (v: string) => void;
  clearRunError: () => void;
  loadRun: (fromPoll: boolean) => void;
  setLastRunRefreshedAt: (d: Date) => void;
  setLastServerTimeUtcSource: (v: string) => void;
  serverNowDate: () => Date;
}): void {
  if ((args.runId() ?? '').trim()) return;

  const fromObs = args.pickActiveRunIdFromCandidates(args.observabilityCandidates() ?? []);
  const fromQueue = args.pickActiveRunIdFromCandidates(args.queueCandidates() ?? []);
  const id = fromObs || fromQueue;
  if (!id) return;

  args.setRunId(id);
  args.setAutoSelectedActiveRunSource(args.source);
  args.clearRunError();
  args.loadRun(false);
  args.setLastRunRefreshedAt(args.serverNowDate());
  args.setLastServerTimeUtcSource(`auto-select/${args.source}`);
}

export function pickActiveRunIdFromCandidatesImpl(candidates: ActiveRunCandidate[]): string {
  const items = (candidates ?? []).filter(x => !!(x?.upgradeRunId ?? '').trim());
  if (items.length === 0) return '';

  const running = items.find(x => (x.state ?? '').toLowerCase().trim() === 'running');
  if (running) return (running.upgradeRunId ?? '').trim();

  const pending = items.find(x => (x.state ?? '').toLowerCase().trim() === 'pending');
  if (pending) return (pending.upgradeRunId ?? '').trim();

  return (items[0].upgradeRunId ?? '').trim();
}
