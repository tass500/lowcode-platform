export function togglePollingImpl(args: {
  pollingEnabled: () => boolean;
  setPollingEnabled: (v: boolean) => void;
  startPolling: () => void;
  stopPolling: () => void;
}): void {
  const next = !args.pollingEnabled();
  args.setPollingEnabled(next);
  if (next)
    args.startPolling();
  else
    args.stopPolling();
}

export function enablePollingImpl(args: {
  pollingEnabled: () => boolean;
  setPollingEnabled: (v: boolean) => void;
  startPolling: () => void;
}): void {
  if (args.pollingEnabled()) return;
  args.setPollingEnabled(true);
  args.startPolling();
}
