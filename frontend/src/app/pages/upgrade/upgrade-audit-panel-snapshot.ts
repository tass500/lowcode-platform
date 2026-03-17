export type AuditPanelSnapshotArgs<TItem> = {
  lastAuditRefreshedAt: Date | null;
  paging: {
    take: number;
    skip: number;
    totalCount: number | null;
    returnedCount: number;
  };
  filters: {
    actor: string;
    actionContains: string;
    traceId: string;
    sinceUtc: string;
  };
  urls: {
    list: string;
    export: string;
  };
  pageItems: TItem[];
};

export function buildAuditPanelSnapshot<TItem>(args: AuditPanelSnapshotArgs<TItem>) {
  return {
    lastAuditRefreshedAtUtc: args.lastAuditRefreshedAt ? args.lastAuditRefreshedAt.toISOString() : null,
    paging: {
      take: args.paging.take || 50,
      skip: args.paging.skip || 0,
      totalCount: args.paging.totalCount,
      returnedCount: args.paging.returnedCount,
    },
    filters: {
      actor: args.filters.actor || null,
      actionContains: args.filters.actionContains || null,
      traceId: args.filters.traceId || null,
      sinceUtc: args.filters.sinceUtc || null,
    },
    urls: {
      list: args.urls.list,
      export: args.urls.export,
    },
    pageItems: args.pageItems,
  };
}
