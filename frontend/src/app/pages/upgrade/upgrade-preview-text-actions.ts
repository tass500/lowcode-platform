import type { ServerBuildInfo } from './upgrade-server-build';

export function serverBuildHeadersPreviewTextImpl(args: {
  serverBuild: ServerBuildInfo | null;
  serverBuildHeaderLines: (serverBuild: ServerBuildInfo) => string[];
}): string {
  if (!args.serverBuild) return '';
  return args.serverBuildHeaderLines(args.serverBuild).join('\n');
}

export function traceHeaderPreviewTextImpl(args: {
  clientTraceHeaderArg: () => string;
}): string {
  return args.clientTraceHeaderArg();
}

export function curlSnippetsPreviewTextImpl(args: {
  curlSnippetsText: () => string;
}): string {
  return args.curlSnippetsText();
}

export function copyTicketHeaderImpl(args: {
  hasRun: boolean;
  ticketHeaderPreviewText: () => string;
  copyText: (text: string) => void | Promise<void>;
}) {
  if (!args.hasRun) return;
  void args.copyText(args.ticketHeaderPreviewText());
}
