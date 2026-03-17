export function auditPrevPageImpl(args: {
  take: number;
  auditSkip: number;
  onAuditFilterChanged: () => void;
  setAuditSkip: (v: number) => void;
  loadAudit: () => void;
}) {
  args.onAuditFilterChanged();
  const take = args.take || 50;
  args.setAuditSkip(Math.max(0, (args.auditSkip || 0) - take));
  args.loadAudit();
}

export function auditNextPageImpl(args: {
  take: number;
  auditSkip: number;
  onAuditFilterChanged: () => void;
  setAuditSkip: (v: number) => void;
  loadAudit: () => void;
}) {
  args.onAuditFilterChanged();
  const take = args.take || 50;
  args.setAuditSkip((args.auditSkip || 0) + take);
  args.loadAudit();
}
