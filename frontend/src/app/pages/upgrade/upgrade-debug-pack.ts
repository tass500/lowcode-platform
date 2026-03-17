import { buildDebugPackFileName } from './upgrade-export-builders';

export function buildDebugPackOutput(args: {
  uiRev: string;
  capturedAtUtc: string;
  serverBuild: any;
  clientTraceId: string;
  selectedRunId: string;
  selectedRunTraceId: string;
  audit: {
    filters: {
      take: number;
      skip: number;
      actor: string | null;
      actionContains: string | null;
      traceId: string | null;
      sinceUtc: string | null;
    };
    urls: {
      list: string;
      export: string;
    };
    exportMax: number;
    exportPreview: any;
    panelSnapshot: any;
  };
  artifacts: {
    ticketMarkdown: string | null;
    curlSnippets: string | null;
    incidentBundle: any;
  };
  fileNameArgs: {
    traceId: string;
    runId: string;
  };
}): { json: string; fileName: string } {
  const pack = {
    uiRev: args.uiRev,
    capturedAtUtc: args.capturedAtUtc,
    serverBuild: args.serverBuild,
    clientTraceId: (args.clientTraceId ?? '').trim() || null,
    selectedRunId: (args.selectedRunId ?? '').trim() || null,
    selectedRunTraceId: (args.selectedRunTraceId ?? '').trim() || null,
    audit: args.audit,
    artifacts: args.artifacts,
  };

  const json = JSON.stringify(pack, null, 2);
  const fileName = buildDebugPackFileName({ traceId: args.fileNameArgs.traceId, runId: args.fileNameArgs.runId });
  return { json, fileName };
}
