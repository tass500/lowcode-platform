import { confirmLargeDownload } from './upgrade-download-utils';

export async function runDebugPackImpl(args: {
  mode: 'copy' | 'download';

  isLoading: () => boolean;
  nextSeq: () => number;
  getSeq: () => number;

  setLoading: (v: boolean) => void;
  setError: (v: string) => void;
  setStep: (v: string) => void;

  buildDebugPack: (seq: number) => Promise<{ json: string; fileName: string }>;

  copyText: (text: string) => Promise<void>;
  downloadJson: (fileName: string, text: string) => void;
  setCopyStatus: (v: string) => void;

  formatError: (e: any, fallback: string) => string;
}): Promise<void> {
  if (args.isLoading()) return;

  const seq = args.nextSeq();
  args.setLoading(true);
  args.setError('');
  args.setStep('starting');

  try {
    const built = await args.buildDebugPack(seq);
    if (seq !== args.getSeq()) return;

    args.setStep('finalizing');

    const okSize = confirmLargeDownload({
      textLength: built.json.length,
      thresholdBytes: 1_000_000,
      buildMessage: ({ kb }) => `Debug pack is ${kb} KB. Continue?`,
    });
    if (!okSize) {
      args.setCopyStatus('Debug pack cancelled.');
      return;
    }

    if (args.mode === 'copy') {
      await args.copyText(built.json);
    } else {
      args.downloadJson(built.fileName, built.json);
      args.setCopyStatus('Downloaded debug pack.');
    }
  } catch (e: any) {
    if (seq !== args.getSeq()) return;
    if ((e?.message ?? '') === 'debug_pack_superseded')
      return;
    args.setError(args.formatError(e, 'Pack failed.'));
  } finally {
    if (seq !== args.getSeq()) return;
    args.setLoading(false);
    args.setStep('');
  }
}
