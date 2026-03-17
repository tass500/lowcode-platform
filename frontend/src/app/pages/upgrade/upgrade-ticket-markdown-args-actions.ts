import { type ServerBuildInfo as ServerBuildInfoType } from './upgrade-server-build';

export function ticketMarkdownArgsImpl(args: {
  shortHeader: string;
  ticketHeaderPreviewText: () => string;
  clientTraceHeaderArgText: () => string;
  serverBuild: ServerBuildInfoType | null;
  curlSnippetsText: () => string;
}): {
  shortHeader: string;
  fullHeader: string;
  serverBuild: ServerBuildInfoType | null;
  traceHeader: string;
  curls: string;
} {
  return {
    shortHeader: args.shortHeader,
    fullHeader: args.ticketHeaderPreviewText(),
    traceHeader: args.clientTraceHeaderArgText(),
    serverBuild: args.serverBuild,
    curls: args.curlSnippetsText(),
  };
}
