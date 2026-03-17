export function setRunIdManuallyImpl(args: {
  idRaw: string;

  getRunId: () => string;
  setRunId: (v: string) => void;

  clearAutoSelectedActiveRun: () => void;

  clearRun: () => void;
  clearRunError: () => void;
  stopPolling: () => void;
}): void {
  const next = (args.idRaw ?? '').trim();
  if (next === (args.getRunId() ?? '').trim()) {
    args.setRunId(next);
    return;
  }

  args.setRunId(next);
  args.clearAutoSelectedActiveRun();

  if (!next) {
    args.clearRun();
    args.clearRunError();
    args.stopPolling();
  }
}
