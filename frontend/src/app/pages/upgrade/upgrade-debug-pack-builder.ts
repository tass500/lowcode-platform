import { toNullableAuditUrlFilters, type NullableAuditUrlFilters } from './upgrade-audit-urls';
import { buildDebugPackOutput } from './upgrade-debug-pack';
import type { AuditUrlFilters } from './upgrade-url-builders';
import type { AuditLogItem, ServerTimedResponse } from './upgrade-types';

export async function buildDebugPackImpl(args: {
  seq: number;
  getSeq: () => number;
  setStep: (step: string) => void;

  buildIncidentBundle: () => Promise<{ payload: any }>;
  buildTicketMarkdownText: () => string;
  curlSnippetsText: () => string;

  audit: {
    previewMax: number;
    urlFilters: AuditUrlFilters;
    buildExportUrlForMax: (max: number) => string;
    httpGetAuditPreview: (url: string) => Promise<ServerTimedResponse<AuditLogItem>>;
    formatError: (e: any, fallback: string) => string;

    buildExportUrl: () => string;
    buildListUrl: () => string;
    buildPanelSnapshot: () => any;

    take: number;
    skip: number;
    exportMax: number;
  };

  packMeta: {
    uiRev: string;
    capturedAtUtc: string;
    serverBuild: any;
    clientTraceId: string;
    selectedRunId: string;
    selectedRunTraceId: string;
    fileNameArgs: {
      traceId: string;
      runId: string;
    };
  };
}): Promise<{ json: string; fileName: string }> {
  const assertSeq = () => {
    if (args.seq !== args.getSeq()) throw new Error('debug_pack_superseded');
  };

  assertSeq();
  args.setStep('incident bundle');
  const incident = await args.buildIncidentBundle();
  assertSeq();

  args.setStep('ticket markdown');
  const ticketMarkdown = args.buildTicketMarkdownText();
  assertSeq();

  args.setStep('curl snippets');
  const curlSnippets = args.curlSnippetsText();
  assertSeq();

  const auditPreviewMax = args.audit.previewMax;
  const auditFiltersNullable: NullableAuditUrlFilters = toNullableAuditUrlFilters(args.audit.urlFilters);
  const auditPreviewUrl = args.audit.buildExportUrlForMax(auditPreviewMax);
  const auditPreviewRequest = {
    url: auditPreviewUrl,
    max: auditPreviewMax,
    actor: auditFiltersNullable.actor,
    actionContains: auditFiltersNullable.actionContains,
    traceId: auditFiltersNullable.traceId,
    sinceUtc: auditFiltersNullable.sinceUtc,
  };

  let auditExportPreview: any = null;
  try {
    args.setStep('audit preview');
    assertSeq();

    const previewRes = await args.audit.httpGetAuditPreview(auditPreviewUrl);
    const previewItems = previewRes?.items ?? [];
    const previewTotal = Number.isFinite(previewRes?.totalCount as any) ? (previewRes?.totalCount ?? 0) : null;

    const truncated = previewTotal != null
      ? previewItems.length < previewTotal
      : previewItems.length >= auditPreviewMax;

    auditExportPreview = {
      ok: true,
      request: auditPreviewRequest,
      maxPreviewTake: auditPreviewMax,
      truncated,
      response: previewRes,
    };
  } catch (e: any) {
    assertSeq();
    auditExportPreview = {
      ok: false,
      request: auditPreviewRequest,
      max: auditPreviewMax,
      maxPreviewTake: auditPreviewMax,
      error: args.audit.formatError(e, 'Preview failed.'),
    };
  }

  const auditExportUrl = args.audit.buildExportUrl();
  const auditListUrl = args.audit.buildListUrl();
  const auditPanelSnapshot = args.audit.buildPanelSnapshot();

  args.setStep('serialize');
  assertSeq();

  return buildDebugPackOutput({
    uiRev: args.packMeta.uiRev,
    capturedAtUtc: args.packMeta.capturedAtUtc,
    serverBuild: args.packMeta.serverBuild,
    clientTraceId: args.packMeta.clientTraceId,
    selectedRunId: args.packMeta.selectedRunId,
    selectedRunTraceId: args.packMeta.selectedRunTraceId,
    audit: {
      filters: {
        take: args.audit.take,
        skip: args.audit.skip,
        ...auditFiltersNullable,
      },
      urls: {
        list: auditListUrl,
        export: auditExportUrl,
      },
      exportMax: args.audit.exportMax,
      exportPreview: auditExportPreview,
      panelSnapshot: auditPanelSnapshot,
    },
    artifacts: {
      ticketMarkdown: ticketMarkdown || null,
      curlSnippets: curlSnippets || null,
      incidentBundle: incident.payload,
    },
    fileNameArgs: {
      traceId: args.packMeta.fileNameArgs.traceId,
      runId: args.packMeta.fileNameArgs.runId,
    }
  });
}
