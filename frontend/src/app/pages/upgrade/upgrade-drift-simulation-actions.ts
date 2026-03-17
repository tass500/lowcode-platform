export function setSimulatedClientDriftMinutesImpl(args: {
  mins: number;

  setSimulatedClientDriftMinutes: (v: number) => void;
  clientNowMs: () => number;
  setNowTick: (v: number) => void;

  serverTimeUtcFromCachedStatus: () => string | null | undefined;
  applyServerTimeUtc: (v: string | null | undefined, source: string) => void;

  loadStatus: () => void;
}): void {
  args.setSimulatedClientDriftMinutes(Math.trunc(args.mins || 0));
  args.setNowTick(args.clientNowMs());

  args.applyServerTimeUtc(args.serverTimeUtcFromCachedStatus(), 'installation/status (cached)');

  args.loadStatus();
}
