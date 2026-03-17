export function copyFullAuditJsonImpl(args: {
  runAuditExport: (mode: 'copy' | 'download') => void | Promise<void>;
}) {
  void args.runAuditExport('copy');
}

export function downloadFullAuditJsonImpl(args: {
  runAuditExport: (mode: 'copy' | 'download') => void | Promise<void>;
}) {
  void args.runAuditExport('download');
}

export function copyCurlSnippetsWiringImpl(args: {
  buildCurlSnippetsText: () => string;
  copyCurlSnippets: (args: { text: string }) => void | Promise<void>;
}) {
  const t = args.buildCurlSnippetsText();
  if (!t) return;
  void args.copyCurlSnippets({ text: t });
}

export function downloadCurlSnippetsWiringImpl(args: {
  buildCurlSnippetsText: () => string;
  runId: string;
  shortId: (id: string) => string;
  downloadCurlSnippets: (args: { text: string; runIdShort: string }) => void;
}) {
  const t = args.buildCurlSnippetsText();
  if (!t) return;

  const runIdShort = args.runId ? args.shortId(args.runId) : 'unknown';
  args.downloadCurlSnippets({ text: t, runIdShort });
}
