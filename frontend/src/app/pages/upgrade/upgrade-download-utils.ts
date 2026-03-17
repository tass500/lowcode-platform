export function buildCompactIsoTimestamp(date: Date = new Date()): string {
  return date.toISOString().replaceAll(':', '').replaceAll('.', '');
}

export function confirmLargeDownload(args: {
  textLength: number;
  thresholdBytes: number;
  buildMessage: (args: { kb: number }) => string;
}): boolean {
  if (args.textLength <= args.thresholdBytes) return true;
  const kb = Math.round(args.textLength / 1024);
  return window.confirm(args.buildMessage({ kb }));
}
