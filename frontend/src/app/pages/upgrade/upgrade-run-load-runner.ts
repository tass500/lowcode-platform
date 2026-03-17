import type {
  ServerTimedSingleResponse,
  UpgradeRun,
} from './upgrade-types';

export async function loadLatestImpl(args: {
  silent: boolean;

  clearAutoSelectedActiveRun: () => void;

  setRunLoading: (v: boolean) => void;
  setRunError: (v: string) => void;
  setRun: (v: ServerTimedSingleResponse<UpgradeRun> | null) => void;
  setRunId: (v: string) => void;
  setLastRunRefreshedAt: (v: Date | null) => void;
  setPollInFlight: (v: boolean) => void;

  fetchLatest: () => Promise<ServerTimedSingleResponse<UpgradeRun>>;
  serverNowDate: () => Date;
  formatError: (e: any, fallback: string) => string;

  isActiveRunState: (state: any) => boolean;
  startPolling: () => void;
  stopPolling: () => void;
}): Promise<void> {
  args.clearAutoSelectedActiveRun();

  const affectLoading = !args.silent;
  if (!args.silent) {
    args.setRunLoading(true);
    args.setRunError('');
  }

  try {
    const res = await args.fetchLatest();
    args.setRun(res);
    args.setRunError('');

    const nextRunId = (res?.upgradeRunId ?? '').trim();
    args.setRunId(nextRunId);

    args.setLastRunRefreshedAt(args.serverNowDate());
    args.setPollInFlight(false);

    if (!nextRunId) {
      if (affectLoading) args.setRunLoading(false);
      args.stopPolling();
      return;
    }

    if (affectLoading) args.setRunLoading(false);
    if (args.isActiveRunState(res?.state))
      args.startPolling();
    else
      args.stopPolling();
  } catch (e: any) {
    if (affectLoading) args.setRunLoading(false);
    args.setPollInFlight(false);
    args.stopPolling();
    args.setRunError(args.formatError(e, 'Failed to load latest run.'));
  }
}

export async function loadRunImpl(args: {
  fromPoll: boolean;

  getRunId: () => string;
  getCurrentRun: () => ServerTimedSingleResponse<UpgradeRun> | null;

  setRunLoading: (v: boolean) => void;
  setRunError: (v: string) => void;
  setRun: (v: ServerTimedSingleResponse<UpgradeRun> | null) => void;
  setLastRunRefreshedAt: (v: Date | null) => void;
  setPollInFlight: (v: boolean) => void;

  fetchRun: (id: string) => Promise<ServerTimedSingleResponse<UpgradeRun>>;
  serverNowDate: () => Date;
  formatError: (e: any, fallback: string) => string;

  isActiveRunState: (state: any) => boolean;
  startPolling: () => void;
  stopPolling: () => void;

  afterBecameTerminal: () => void;
}): Promise<void> {
  const id = (args.getRunId() ?? '').trim();
  if (!id) return;

  const affectLoading = !args.fromPoll;
  if (!args.fromPoll) {
    args.setRunLoading(true);
    args.setRunError('');
  }

  try {
    const prevState = (args.getCurrentRun()?.state ?? '').toLowerCase().trim();

    const res = await args.fetchRun(id);
    args.setRun(res);
    args.setRunError('');
    args.setLastRunRefreshedAt(args.serverNowDate());

    if (affectLoading) args.setRunLoading(false);
    args.setPollInFlight(false);

    const nextState = (res?.state ?? '').toLowerCase().trim();
    if (args.isActiveRunState(nextState))
      args.startPolling();
    else
      args.stopPolling();

    if (args.isActiveRunState(prevState) && !args.isActiveRunState(nextState)) {
      args.afterBecameTerminal();
    }
  } catch (e: any) {
    if (!args.fromPoll) args.setRun(null);
    if (affectLoading) args.setRunLoading(false);
    args.setPollInFlight(false);
    args.stopPolling();
    args.setRunError(args.formatError(e, 'Failed to load run.'));
  }
}
