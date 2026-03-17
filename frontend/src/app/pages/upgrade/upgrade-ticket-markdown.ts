import { ServerBuildInfo, serverBuildHeaderLines } from './upgrade-server-build';

export function buildTicketMarkdownInline(args: {
  shortHeader: string;
  fullHeader: string;
  serverBuild: ServerBuildInfo | null;
  traceHeader: string;
  curls: string;
}): string {
  const shortHeader = (args.shortHeader ?? '').trim();
  if (!shortHeader) return '';

  const fullHeader = args.fullHeader ?? '';
  const traceHeader = args.traceHeader ?? '';
  const curls = args.curls ?? '';

  const lines: string[] = [];
  lines.push(`**${shortHeader}**`);
  lines.push('');
  lines.push(fullHeader);
  lines.push('');
  if (args.serverBuild) {
    lines.push('```');
    lines.push(...serverBuildHeaderLines(args.serverBuild));
    lines.push('```');
    lines.push('');
  }
  if (traceHeader) {
    lines.push(traceHeader);
    lines.push('');
  }
  if (curls) {
    lines.push('```bash');
    lines.push(curls);
    lines.push('```');
    lines.push('');
  }

  return lines.join('\n');
}

export function buildTicketMarkdownSectioned(args: {
  shortHeader: string;
  fullHeader: string;
  serverBuild: ServerBuildInfo | null;
  traceHeader: string;
  curls: string;
}): string {
  const shortHeader = (args.shortHeader ?? '').trim();
  const fullHeader = args.fullHeader ?? '';
  const traceHeader = args.traceHeader ?? '';
  const curls = args.curls ?? '';

  const lines: string[] = [];
  if (shortHeader) lines.push(`**${shortHeader}**`);
  lines.push('');
  lines.push('### Debug header');
  lines.push('```');
  lines.push(fullHeader);
  lines.push('```');
  lines.push('');

  if (args.serverBuild) {
    lines.push('### Server build');
    lines.push('```');
    lines.push(...serverBuildHeaderLines(args.serverBuild));
    lines.push('```');
    lines.push('');
  }
  if (traceHeader) {
    lines.push('### Trace header');
    lines.push('```');
    lines.push(traceHeader);
    lines.push('```');
    lines.push('');
  }
  if (curls) {
    lines.push('### Curl snippets');
    lines.push('```bash');
    lines.push(curls);
    lines.push('```');
    lines.push('');
  }

  return lines.join('\n');
}
