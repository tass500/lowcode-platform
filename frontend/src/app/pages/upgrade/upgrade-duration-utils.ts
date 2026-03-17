import { parseDateUtc } from './upgrade-time-utils';

export function durationMsFromStartedAtUtc(args: {
  startedAtUtc: string | null | undefined;
  nowServerMs: number;
}): number | null {
  const started = parseDateUtc(args.startedAtUtc);
  if (!started) return null;
  return args.nowServerMs - started.getTime();
}

export function durationMsFromStartEndUtc(args: {
  startedAtUtc: string | null | undefined;
  finishedAtUtc: string | null | undefined;
  nowServerMs: number;
}): number | null {
  const started = parseDateUtc(args.startedAtUtc);
  if (!started) return null;

  const finished = parseDateUtc(args.finishedAtUtc);
  const endMs = finished ? finished.getTime() : args.nowServerMs;
  return endMs - started.getTime();
}
