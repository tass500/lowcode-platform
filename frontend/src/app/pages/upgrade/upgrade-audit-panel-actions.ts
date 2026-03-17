export function copyAuditPageJsonImpl(args: {
  auditTake: number;
  auditSkip: number;
  auditActor: string;
  auditActionContains: string;
  auditTraceId: string;
  auditSinceUtc: string;
  auditTotalCount: number | null;
  auditItems: unknown[];
  copyText: (text: string) => void | Promise<void>;
}) {
  if (args.auditItems.length === 0) return;

  const t = JSON.stringify({
    take: args.auditTake || 50,
    skip: args.auditSkip,
    actor: args.auditActor || null,
    actionContains: args.auditActionContains || null,
    traceId: args.auditTraceId || null,
    sinceUtc: args.auditSinceUtc || null,
    totalCount: args.auditTotalCount,
    items: args.auditItems,
  }, null, 2);

  void args.copyText(t);
}

export function clearAuditFiltersImpl(args: {
  setAuditTake: (v: number) => void;
  setAuditSkip: (v: number) => void;
  setAuditActor: (v: string) => void;
  setAuditActionContains: (v: string) => void;
  setAuditTraceId: (v: string) => void;
  setAuditSinceUtc: (v: string) => void;
  setAuditTotalCount: (v: number | null) => void;
  setAuditItems: (v: unknown[]) => void;
  onAuditFilterChanged: () => void;
  loadAudit: () => void;
}) {
  args.setAuditTake(50);
  args.setAuditSkip(0);
  args.setAuditActor('');
  args.setAuditActionContains('');
  args.setAuditTraceId('');
  args.setAuditSinceUtc('');
  args.setAuditTotalCount(null);
  args.setAuditItems([]);
  args.onAuditFilterChanged();
  args.loadAudit();
}
