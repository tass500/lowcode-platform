export function offsetMinutesAbsImpl(serverNowOffsetMs: number): number {
  return Math.round(Math.abs(serverNowOffsetMs) / 60_000);
}

export function simulatedClientDriftMsImpl(simulatedClientDriftMinutes: number): number {
  return Math.trunc(simulatedClientDriftMinutes || 0) * 60_000;
}

export function clientNowMsImpl(args: {
  simulatedClientDriftMinutes: number;
  nowMs: () => number;
}): number {
  return args.nowMs() + simulatedClientDriftMsImpl(args.simulatedClientDriftMinutes);
}

export function applyServerTimeUtcImpl(args: {
  serverTimeUtc: string | null | undefined;
  source: string;

  parseDateUtc: (v: string | null | undefined) => Date | null;
  clientNowMs: () => number;

  setServerNowOffsetMs: (v: number) => void;
  setLastServerTimeUtcApplied: (v: string) => void;
  setLastServerTimeUtcSource: (v: string) => void;
}): void {
  const serverTime = args.parseDateUtc(args.serverTimeUtc);
  if (!serverTime) return;

  args.setServerNowOffsetMs(serverTime.getTime() - args.clientNowMs());
  args.setLastServerTimeUtcApplied(serverTime.toISOString());
  args.setLastServerTimeUtcSource(args.source);
}
