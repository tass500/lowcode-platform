import { confirmLargeDownload } from './upgrade-download-utils';
import { buildCurlSnippetsFileName } from './upgrade-export-builders';
import { downloadTextFilePlain } from './upgrade-ui-utils';

export function downloadCurlSnippetsImpl(args: {
  text: string;
  runIdShort: string;
  setCopyStatus: (v: string) => void;
}): boolean {
  const okSize = confirmLargeDownload({
    textLength: args.text.length,
    thresholdBytes: 200_000,
    buildMessage: ({ kb }) => `Curl snippets are ${kb} KB. Continue download?`,
  });
  if (!okSize) return false;

  const fileName = buildCurlSnippetsFileName({ runIdShort: args.runIdShort });
  downloadTextFilePlain(fileName, args.text, 'text/plain;charset=utf-8');
  args.setCopyStatus('Downloaded curl snippets.');
  return true;
}
