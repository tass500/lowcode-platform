import { confirmLargeDownload } from './upgrade-download-utils';

export async function runIncidentBundleExportImpl(args: {
  mode: 'copy' | 'download';

  isLoading: () => boolean;
  resetUpgradeActionState: () => void;

  setLoading: (v: boolean) => void;
  setError: (v: string) => void;

  buildIncidentBundle: () => Promise<{ jsonText: string; text: string; fileName: string }>;

  copyText: (text: string) => Promise<void>;
  downloadJson: (fileName: string, text: string) => void;
  setCopyStatus: (v: string) => void;
}): Promise<void> {
  if (args.isLoading()) return;
  args.resetUpgradeActionState();
  args.setLoading(true);

  try {
    const built = await args.buildIncidentBundle();

    const okSize = confirmLargeDownload({
      textLength: built.jsonText.length,
      thresholdBytes: 1_000_000,
      buildMessage: ({ kb }) => `Incident bundle is ${kb} KB. Continue?`,
    });
    if (!okSize) return;

    if (args.mode === 'copy') {
      await args.copyText(built.text);
    } else {
      args.downloadJson(built.fileName, built.jsonText);
      args.setCopyStatus('Downloaded incident bundle.');
    }
  } catch (e: any) {
    args.setError(e?.message ? `Bundle failed: ${e.message}` : 'Bundle failed.');
  } finally {
    args.setLoading(false);
  }
}
