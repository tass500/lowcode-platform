import { buildAuditExportUrl, buildAuditListUrl, type AuditUrlFilters } from './upgrade-url-builders';

export type NullableAuditUrlFilters = {
  actor: string | null;
  actionContains: string | null;
  traceId: string | null;
  sinceUtc: string | null;
};

export function normalizeAuditUrlFilters(filters: AuditUrlFilters): AuditUrlFilters {
  return {
    actor: (filters.actor ?? '').trim(),
    actionContains: (filters.actionContains ?? '').trim(),
    traceId: (filters.traceId ?? '').trim(),
    sinceUtc: (filters.sinceUtc ?? '').trim(),
  };
}

export function toNullableAuditUrlFilters(filters: AuditUrlFilters): NullableAuditUrlFilters {
  const f = normalizeAuditUrlFilters(filters);
  return {
    actor: f.actor || null,
    actionContains: f.actionContains || null,
    traceId: f.traceId || null,
    sinceUtc: f.sinceUtc || null,
  };
}

export function buildAuditUrls(args: {
  take: number;
  skip: number;
  max: number;
  filters: AuditUrlFilters;
}): { list: string; export: string } {
  const filters = normalizeAuditUrlFilters(args.filters);
  return {
    list: buildAuditListUrl({
      take: args.take || 50,
      skip: args.skip || 0,
      filters,
    }),
    export: buildAuditExportUrl({
      max: args.max || 5000,
      filters,
    }),
  };
}

export function buildAuditExportUrlForMax(args: {
  max: number;
  filters: AuditUrlFilters;
}): string {
  const filters = normalizeAuditUrlFilters(args.filters);
  return buildAuditExportUrl({
    max: args.max || 5000,
    filters,
  });
}
