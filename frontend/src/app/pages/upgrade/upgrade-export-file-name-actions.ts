export function auditExportFileNameImpl(args: {
  auditTraceId: string;
  auditActor: string;
  buildAuditExportFileName: (a: { auditTraceId: string; auditActor: string }) => string;
}): string {
  return args.buildAuditExportFileName({ auditTraceId: args.auditTraceId, auditActor: args.auditActor });
}

export function debugPackFileNameImpl(args: {
  traceId: string;
  runId: string;
  buildDebugPackFileName: (a: { traceId: string; runId: string }) => string;
}): string {
  return args.buildDebugPackFileName({ traceId: args.traceId, runId: args.runId });
}
