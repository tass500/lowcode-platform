export function refreshAllImpl(args: {
  loadStatus: () => void;
  loadObservability: () => void;
  loadQueue: () => void;
  loadRecentRuns: () => void;
  loadAudit: () => void;

  runId: () => string;
  loadRun: () => void;
  loadLatest: (silent: boolean) => void;
}): void {
  args.loadStatus();
  args.loadObservability();
  args.loadQueue();
  args.loadRecentRuns();
  args.loadAudit();

  if ((args.runId() ?? '').trim()) {
    args.loadRun();
  } else {
    args.loadLatest(false);
  }
}
