export function queueRunDurationMsImpl(args: {
  startedAtUtc: string | null;
  nowServerMs: number;
  durationMsFromStartedAtUtc: (args: { startedAtUtc: string | null; nowServerMs: number }) => number | null;
}): number | null {
  return args.durationMsFromStartedAtUtc({
    startedAtUtc: args.startedAtUtc,
    nowServerMs: args.nowServerMs,
  });
}

export function recentRunDurationMsImpl(args: {
  startedAtUtc: string | null;
  finishedAtUtc: string | null;
  nowServerMs: number;
  durationMsFromStartEndUtc: (args: { startedAtUtc: string | null; finishedAtUtc: string | null; nowServerMs: number }) => number | null;
}): number | null {
  return args.durationMsFromStartEndUtc({
    startedAtUtc: args.startedAtUtc,
    finishedAtUtc: args.finishedAtUtc,
    nowServerMs: args.nowServerMs,
  });
}

export function runDurationMsImpl(args: {
  startedAtUtc: string | null;
  finishedAtUtc: string | null;
  nowServerMs: number;
  durationMsFromStartEndUtc: (args: { startedAtUtc: string | null; finishedAtUtc: string | null; nowServerMs: number }) => number | null;
}): number | null {
  return args.durationMsFromStartEndUtc({
    startedAtUtc: args.startedAtUtc,
    finishedAtUtc: args.finishedAtUtc,
    nowServerMs: args.nowServerMs,
  });
}

export function stepDurationMsImpl(args: {
  startedAtUtc: string | null;
  finishedAtUtc: string | null;
  nowServerMs: number;
  durationMsFromStartEndUtc: (args: { startedAtUtc: string | null; finishedAtUtc: string | null; nowServerMs: number }) => number | null;
}): number | null {
  return args.durationMsFromStartEndUtc({
    startedAtUtc: args.startedAtUtc,
    finishedAtUtc: args.finishedAtUtc,
    nowServerMs: args.nowServerMs,
  });
}
