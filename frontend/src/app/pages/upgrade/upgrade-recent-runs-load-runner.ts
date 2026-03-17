import type { RecentUpgradeRun, ServerTimedResponse } from './upgrade-types';

export async function loadRecentRunsImpl(args: {
  setLoading: (v: boolean) => void;
  setError: (v: string) => void;
  setRuns: (v: RecentUpgradeRun[]) => void;

  fetch: () => Promise<ServerTimedResponse<RecentUpgradeRun>>;
  formatError: (e: any, fallback: string) => string;
}): Promise<void> {
  args.setLoading(true);
  args.setError('');

  try {
    const res = await args.fetch();
    args.setRuns(res.items ?? []);
    args.setLoading(false);
  } catch (e: any) {
    args.setRuns([]);
    args.setLoading(false);
    args.setError(args.formatError(e, 'Failed to load recent runs.'));
  }
}
