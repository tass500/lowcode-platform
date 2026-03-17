import type { ServerBuildInfo } from './upgrade-server-build';

type TicketMarkdownArgs = {
  shortHeader: string;
  fullHeader: string;
  serverBuild: ServerBuildInfo | null;
  traceHeader: string;
  curls: string;
};

export function copyTicketMarkdownWiringImpl(args: {
  hasRun: () => boolean;

  buildShortHeader: () => string;
  ticketMarkdownArgs: (shortHeader: string) => TicketMarkdownArgs;
  buildTicketMarkdownSectioned: (a: TicketMarkdownArgs) => string;

  copyTicketMarkdown: (a: { text: string }) => void;
}): void {
  if (!args.hasRun()) return;

  const shortHeader = args.buildShortHeader();
  const text = args.buildTicketMarkdownSectioned(args.ticketMarkdownArgs(shortHeader));
  args.copyTicketMarkdown({ text });
}

export function downloadTicketMarkdownWiringImpl(args: {
  hasRun: () => boolean;

  buildShortHeader: () => string;
  ticketMarkdownArgs: (shortHeader: string) => TicketMarkdownArgs;
  buildTicketMarkdownInline: (a: TicketMarkdownArgs) => string;

  runId: () => string;
  shortId: (id: string) => string;

  downloadTicketMarkdown: (a: { text: string; runIdShort: string }) => void;
}): void {
  if (!args.hasRun()) return;

  const shortHeader = args.buildShortHeader();
  if (!shortHeader) return;

  const text = args.buildTicketMarkdownInline(args.ticketMarkdownArgs(shortHeader));
  const runId = (args.runId() ?? '').trim();
  const runIdShort = runId ? args.shortId(runId) : 'unknown';

  args.downloadTicketMarkdown({ text, runIdShort });
}
