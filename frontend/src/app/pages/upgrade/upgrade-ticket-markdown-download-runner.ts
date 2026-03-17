import { confirmLargeDownload } from './upgrade-download-utils';
import { buildTicketMarkdownFileName } from './upgrade-export-builders';
import { downloadTextFilePlain } from './upgrade-ui-utils';

export function downloadTicketMarkdownImpl(args: {
  text: string;
  runIdShort: string;
  setCopyStatus: (v: string) => void;
}): boolean {
  const okSize = confirmLargeDownload({
    textLength: args.text.length,
    thresholdBytes: 200_000,
    buildMessage: ({ kb }) => `Ticket markdown is ${kb} KB. Continue download?`,
  });
  if (!okSize) return false;

  const fileName = buildTicketMarkdownFileName({ runIdShort: args.runIdShort });
  downloadTextFilePlain(fileName, args.text, 'text/markdown;charset=utf-8');
  args.setCopyStatus('Downloaded ticket markdown.');
  return true;
}
