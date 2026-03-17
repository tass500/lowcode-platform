export type IntervalHandle = number;

export function startNowTick(args: {
  nowTickHandle: IntervalHandle | null;
  setNowTickHandle: (h: IntervalHandle | null) => void;
  setNowTick: (v: number) => void;
  clientNowMs: () => number;
}): void {
  if (args.nowTickHandle !== null) return;
  const h = window.setInterval(() => args.setNowTick(args.clientNowMs()), 1000);
  args.setNowTickHandle(h);
}

export function stopNowTick(args: {
  nowTickHandle: IntervalHandle | null;
  setNowTickHandle: (h: IntervalHandle | null) => void;
}): void {
  if (args.nowTickHandle === null) return;
  window.clearInterval(args.nowTickHandle);
  args.setNowTickHandle(null);
}

export function startUpgradeRunPolling(args: {
  pollingEnabled: () => boolean;
  pollHandle: IntervalHandle | null;
  setPollHandle: (h: IntervalHandle | null) => void;

  runId: () => string;
  isActiveRunState: (state: string | null | undefined) => boolean;
  runState: () => string | null | undefined;

  pollInFlight: () => boolean;
  setPollInFlight: (v: boolean) => void;

  setPollTick: (v: number) => void;
  setLastPollAt: (v: Date | null) => void;
  incPollTick: () => number;

  clientNowMs: () => number;

  loadRun: (fromPoll: boolean) => void;
  loadQueue: () => void;
  loadObservability: () => void;

  stopPolling: () => void;
}): void {
  if (!args.pollingEnabled()) return;
  if (args.pollHandle !== null) return;
  if (!args.isActiveRunState(args.runState())) return;

  args.setPollTick(0);
  args.setLastPollAt(null);
  args.setPollInFlight(false);

  const h = window.setInterval(() => {
    if (!args.pollingEnabled()) return;
    if (!args.runId()) {
      args.stopPolling();
      return;
    }
    if (!args.isActiveRunState(args.runState())) {
      args.stopPolling();
      return;
    }
    if (args.pollInFlight()) return;

    const nextTick = args.incPollTick();
    args.setLastPollAt(new Date(args.clientNowMs()));
    args.setPollInFlight(true);

    args.loadRun(true);
    if (nextTick % 3 === 0) {
      args.loadQueue();
      args.loadObservability();
    }
  }, 5000);

  args.setPollHandle(h);
}

export function stopUpgradeRunPolling(args: {
  pollHandle: IntervalHandle | null;
  setPollHandle: (h: IntervalHandle | null) => void;
  setPollTick: (v: number) => void;
  setLastPollAt: (v: Date | null) => void;
  setPollInFlight: (v: boolean) => void;
}): void {
  if (args.pollHandle !== null) {
    window.clearInterval(args.pollHandle);
    args.setPollHandle(null);
  }
  args.setPollTick(0);
  args.setLastPollAt(null);
  args.setPollInFlight(false);
}
