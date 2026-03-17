export function isRefreshingAllImpl(args: {
  statusLoading: boolean;
  observabilityLoading: boolean;
  queueLoading: boolean;
  recentLoading: boolean;
  auditLoading: boolean;
  runLoading: boolean;
}): boolean {
  return !!(
    args.statusLoading ||
    args.observabilityLoading ||
    args.queueLoading ||
    args.recentLoading ||
    args.auditLoading ||
    args.runLoading
  );
}
