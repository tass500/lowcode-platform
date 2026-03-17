import { AuditLogItem, ServerTimedResponse } from './upgrade-types';

import { buildAuditExportText } from './upgrade-export-builders';

export function buildAuditExportTextImpl(args: {
  res: ServerTimedResponse<AuditLogItem>;
  max: number;
  actor: string;
  actionContains: string;
  traceId: string;
  sinceUtc: string;
}): { text: string; totalCount: number | null; returnedCount: number } {
  return buildAuditExportText({
    res: args.res,
    max: args.max,
    actor: args.actor,
    actionContains: args.actionContains,
    traceId: args.traceId,
    sinceUtc: args.sinceUtc,
  });
}

export function mapAuditExportErrorImpl(args: {
  e: any;
  formatError: (e: any, fallback: string) => string;
}): string {
  const status = args.e?.status;
  const code = args.e?.error?.errorCode;
  if (status === 413 || code === 'export_too_large')
    return 'Export is too large for the server. Reduce Export max or narrow filters.';
  return args.formatError(args.e, 'Failed to export audit.');
}
