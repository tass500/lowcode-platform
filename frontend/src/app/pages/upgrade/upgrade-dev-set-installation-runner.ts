export async function devSetInstallationImpl(args: {
  body: any;

  setDevLoading: (v: boolean) => void;
  setDevError: (v: string) => void;

  postDevSet: (body: any) => Promise<{ serverTimeUtc?: string; status: string }>;

  loadStatus: () => void;
  loadObservability: () => void;
  loadQueue: () => void;
  loadAudit: () => void;

  formatError: (e: any, fallback: string) => string;
}): Promise<void> {
  args.setDevLoading(true);
  args.setDevError('');

  try {
    await args.postDevSet(args.body);
    args.setDevLoading(false);
    args.loadStatus();
    args.loadObservability();
    args.loadQueue();
    args.loadAudit();
  } catch (e: any) {
    args.setDevLoading(false);
    args.setDevError(args.formatError(e, 'Dev set failed.'));
  }
}
