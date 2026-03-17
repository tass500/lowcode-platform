export function copyAuditTraceIdImpl(args: {
  traceId: string;
  copyText: (text: string) => void | Promise<void>;
}) {
  const t = (args.traceId ?? '').trim();
  if (!t) return;
  void args.copyText(t);
}

export function copyAuditDetailsImpl(args: {
  detailsJson: string | null;
  copyText: (text: string) => void | Promise<void>;
}) {
  const t = (args.detailsJson ?? '').trim();
  if (!t) return;
  void args.copyText(t);
}

export function copyAuditCurlByTraceIdImpl(args: {
  traceId: string;
  buildAuditCurlByTraceId: (traceId: string) => string;
  copyText: (text: string) => void | Promise<void>;
}) {
  const curl = args.buildAuditCurlByTraceId(args.traceId);
  if (!curl) return;
  void args.copyText(curl);
}
