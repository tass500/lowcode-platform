import type { QueueUpgradeRun, ServerTimedResponse } from './upgrade-types';

export async function loadQueueImpl(args: {
  setLoading: (v: boolean) => void;
  setError: (v: string) => void;
  setRuns: (v: QueueUpgradeRun[]) => void;
  setLastRefreshedAt: (v: Date | null) => void;

  fetch: () => Promise<ServerTimedResponse<QueueUpgradeRun>>;
  serverNowDate: () => Date;
  formatError: (e: any, fallback: string) => string;

  afterSuccess: () => void;
}): Promise<void> {
  args.setLoading(true);
  args.setError('');

  try {
    const res = await args.fetch();
    args.setRuns(res.items ?? []);
    args.setLastRefreshedAt(args.serverNowDate());
    args.setLoading(false);
    args.afterSuccess();
  } catch (e: any) {
    args.setRuns([]);
    args.setLoading(false);
    args.setError(args.formatError(e, 'Failed to load queue.'));
  }
}
