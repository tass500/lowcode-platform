export type AuditUrlFilters = {
  actor: string;
  actionContains: string;
  traceId: string;
  sinceUtc: string;
};

export function toAbsoluteUrl(url: string): string {
  const u = (url ?? '').trim();
  if (!u) return '';
  if (u.startsWith('http://') || u.startsWith('https://')) return u;
  return new URL(u, window.location.origin).toString();
}

export function buildAuditListUrl(args: {
  take: number;
  skip: number;
  filters: AuditUrlFilters;
}): string {
  const params = new URLSearchParams();
  params.set('take', String(args.take || 50));

  if ((args.skip || 0) > 0) params.set('skip', String(args.skip));

  addAuditFilterParams(params, args.filters);

  return `/api/admin/audit?${params.toString()}`;
}

export function buildAuditExportUrl(args: {
  max: number;
  filters: AuditUrlFilters;
}): string {
  const params = new URLSearchParams();
  params.set('max', String(args.max || 5000));

  addAuditFilterParams(params, args.filters);

  return `/api/admin/audit/export?${params.toString()}`;
}

function addAuditFilterParams(params: URLSearchParams, filters: AuditUrlFilters) {
  if (filters.actor) params.set('actor', filters.actor);
  if (filters.actionContains) params.set('actionContains', filters.actionContains);
  if (filters.traceId) params.set('traceId', filters.traceId);
  if (filters.sinceUtc) params.set('sinceUtc', filters.sinceUtc);
}
