export async function startUpgradeImpl(args: {
  targetVersion: string;

  resetUpgradeActionState: () => void;
  setStartLoading: (v: boolean) => void;
  setLastRunId: (v: string) => void;

  postStart: (targetVersion: string) => Promise<{ upgradeRunId?: string }>;

  onStarted: (upgradeRunId: string) => void;
  serverNowDate: () => Date;
  setLastRunRefreshedAt: (v: Date | null) => void;

  loadQueue: () => void;
  loadRecentRuns: () => void;
  loadAudit: () => void;
  loadRun: () => void;

  mapEnforcementBlockError: (e: any) => string;
  setStartError: (v: string) => void;
}): Promise<void> {
  const targetVersion = (args.targetVersion ?? '').trim();
  if (!targetVersion) return;

  args.resetUpgradeActionState();
  args.setStartLoading(true);
  args.setLastRunId('');

  try {
    const res = await args.postStart(targetVersion);
    args.setStartLoading(false);

    const id = (res?.upgradeRunId ?? '').trim();
    args.onStarted(id);
    args.setLastRunId(id);
    args.setLastRunRefreshedAt(args.serverNowDate());

    args.loadQueue();
    args.loadRecentRuns();
    args.loadAudit();
    args.loadRun();
  } catch (e: any) {
    args.setStartLoading(false);
    args.setStartError(args.mapEnforcementBlockError(e));
  }
}

export async function cancelRunImpl(args: {
  runId: string;

  resetUpgradeActionState: () => void;
  setCancelLoading: (v: boolean) => void;

  postCancel: (runId: string) => Promise<{ serverTimeUtc?: string; status: string }>;

  clearRunError: () => void;
  loadRun: () => void;
  loadRecentRuns: () => void;
  loadQueue: () => void;
  loadAudit: () => void;

  formatError: (e: any, fallback: string) => string;
  setRunError: (v: string) => void;
}): Promise<void> {
  const id = (args.runId ?? '').trim();
  if (!id) return;

  args.resetUpgradeActionState();
  args.setCancelLoading(true);

  try {
    await args.postCancel(id);
    args.setCancelLoading(false);
    args.clearRunError();
    args.loadRun();
    args.loadRecentRuns();
    args.loadQueue();
    args.loadAudit();
  } catch (e: any) {
    args.setCancelLoading(false);
    args.setRunError(args.formatError(e, 'Cancel failed.'));
  }
}

export async function retryRunImpl(args: {
  runId: string;

  resetUpgradeActionState: () => void;
  setRetryLoading: (v: boolean) => void;

  postRetry: (runId: string) => Promise<{ serverTimeUtc?: string; status: string }>;

  clearRunError: () => void;
  loadRun: () => void;
  loadRecentRuns: () => void;
  loadQueue: () => void;
  loadAudit: () => void;

  mapEnforcementBlockError: (e: any) => string;
  setRunError: (v: string) => void;
}): Promise<void> {
  const id = (args.runId ?? '').trim();
  if (!id) return;

  args.resetUpgradeActionState();
  args.setRetryLoading(true);

  try {
    await args.postRetry(id);
    args.setRetryLoading(false);
    args.clearRunError();
    args.loadRun();
    args.loadRecentRuns();
    args.loadQueue();
    args.loadAudit();
  } catch (e: any) {
    args.setRetryLoading(false);
    args.setRunError(args.mapEnforcementBlockError(e));
  }
}

export async function devFailStepImpl(args: {
  runId: string;
  stepKey: string;

  resetUpgradeActionState: () => void;
  setDevFailLoading: (v: boolean) => void;

  postDevFailStep: (runId: string, stepKey: string) => Promise<{ serverTimeUtc?: string; status: string }>;

  clearDevFailError: () => void;
  loadRun: () => void;
  loadAudit: () => void;

  formatError: (e: any, fallback: string) => string;
  setDevFailError: (v: string) => void;
}): Promise<void> {
  const id = (args.runId ?? '').trim();
  if (!id) return;

  args.resetUpgradeActionState();
  args.setDevFailLoading(true);

  try {
    await args.postDevFailStep(id, args.stepKey);
    args.setDevFailLoading(false);
    args.clearDevFailError();
    args.loadRun();
    args.loadAudit();
  } catch (e: any) {
    args.setDevFailLoading(false);
    args.setDevFailError(args.formatError(e, 'Failed to request dev fail step.'));
  }
}
