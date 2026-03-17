import { scheduleWatchdogTimeout } from './upgrade-timeouts';
import type { AuditLogItem, ServerTimedResponse } from './upgrade-types';

export async function loadAuditImpl(args: {
  nextSeq: () => number;
  getSeq: () => number;

  isLoading: () => boolean;

  setLoading: (v: boolean) => void;
  setError: (v: string) => void;
  getError: () => string;
  setItems: (v: AuditLogItem[]) => void;
  setTotalCount: (v: number | null) => void;
  setLastRefreshedAt: (v: Date | null) => void;

  buildUrl: () => string;
  fetch: (url: string) => Promise<ServerTimedResponse<AuditLogItem>>;
  serverNowDate: () => Date;
  formatError: (e: any, fallback: string) => string;
}): Promise<void> {
  const seq = args.nextSeq();
  args.setLoading(true);
  args.setError('');
  const url = args.buildUrl();

  scheduleWatchdogTimeout({
    timeoutMs: 30_000,
    shouldApply: () => seq === args.getSeq() && args.isLoading(),
    apply: () => {
      args.setLoading(false);
      if (!args.getError())
        args.setError('Audit request timed out.');
    },
  });

  try {
    const body = await args.fetch(url);
    if (seq !== args.getSeq()) return;

    args.setItems(body?.items ?? []);
    args.setTotalCount(Number.isFinite(body?.totalCount as any) ? (body?.totalCount ?? 0) : null);
    args.setLastRefreshedAt(args.serverNowDate());
    args.setLoading(false);
  } catch (e: any) {
    if (seq !== args.getSeq()) return;

    args.setItems([]);
    args.setTotalCount(null);
    args.setLoading(false);
    args.setError(args.formatError(e, 'Failed to load audit.'));
  }
}
