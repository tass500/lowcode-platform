export type TimeoutHandle = number;

export function setTransientStatus(args: {
  message: string;
  setMessage: (v: string) => void;
  handle: TimeoutHandle | null;
  setHandle: (h: TimeoutHandle | null) => void;
  clearAfterMs: number;
}): void {
  args.setMessage(args.message);
  if (args.handle !== null)
    window.clearTimeout(args.handle);
  const h = window.setTimeout(() => args.setMessage(''), args.clearAfterMs);
  args.setHandle(h);
}

export function scheduleWatchdogTimeout(args: {
  timeoutMs: number;
  shouldApply: () => boolean;
  apply: () => void;
}): void {
  window.setTimeout(() => {
    if (!args.shouldApply()) return;
    args.apply();
  }, args.timeoutMs);
}
