import { type ServerBuildInfo as ServerBuildInfoType } from './upgrade-server-build';

export function buildTicketMarkdownTextImpl(args: {
  run: unknown;
  buildShortHeader: () => string;
  ticketMarkdownArgs: (shortHeader: string) => {
    shortHeader: string;
    fullHeader: string;
    serverBuild: ServerBuildInfoType | null;
    traceHeader: string;
    curls: string;
  };
  buildTicketMarkdownInline: (a: {
    shortHeader: string;
    fullHeader: string;
    serverBuild: ServerBuildInfoType | null;
    traceHeader: string;
    curls: string;
  }) => string;
}): string {
  if (!args.run) return '';

  const shortHeader = args.buildShortHeader();
  if (!shortHeader) return '';

  const a = args.ticketMarkdownArgs(shortHeader);
  return args.buildTicketMarkdownInline(a);
}
