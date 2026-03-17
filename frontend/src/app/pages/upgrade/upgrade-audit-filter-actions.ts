export function applyAuditFiltersImpl(args: {
  onAuditFilterChanged: () => void;
  setAuditSkip: (v: number) => void;
  loadAudit: () => void;
}) {
  args.onAuditFilterChanged();
  args.setAuditSkip(0);
  args.loadAudit();
}

export function auditOnlyUpgradesImpl(args: {
  setAuditSkip: (v: number) => void;
  setAuditActionContains: (v: string) => void;
  applyAuditFilters: () => void;
}) {
  args.setAuditSkip(0);
  args.setAuditActionContains('upgrade_');
  args.applyAuditFilters();
}

export function auditOnlyCurrentRunImpl(args: {
  runTraceId: string;
  setAuditSkip: (v: number) => void;
  setAuditTraceId: (v: string) => void;
  setAuditActionContains: (v: string) => void;
  applyAuditFilters: () => void;
}): boolean {
  const t = (args.runTraceId ?? '').trim();
  if (!t) return false;

  args.setAuditSkip(0);
  args.setAuditTraceId(t);
  args.setAuditActionContains('');
  args.applyAuditFilters();
  return true;
}
