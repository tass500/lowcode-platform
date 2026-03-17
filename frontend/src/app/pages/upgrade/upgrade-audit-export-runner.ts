import type { AuditLogItem, ServerTimedResponse } from './upgrade-types';

export async function runAuditExportImpl(args: {
  mode: 'copy' | 'download';

  exportMax: number;
  getExportFileName: () => string;

  confirmOrCancel: (message: string) => boolean;

  resetUpgradeActionState: () => void;

  setExportLoading: (v: boolean) => void;
  setExportError: (v: string) => void;
  setExportInfo: (v: string) => void;

  fetchAuditExport: () => Promise<ServerTimedResponse<AuditLogItem>>;
  buildAuditExportText: (res: ServerTimedResponse<AuditLogItem>) => {
    text: string;
    totalCount: number | null;
    returnedCount: number;
  };

  copyText: (text: string) => Promise<void>;
  downloadJson: (fileName: string, text: string) => void;

  mapAuditExportError: (e: any) => string;
}): Promise<void> {
  if ((args.exportMax || 0) > 10_000) {
    const ok = args.confirmOrCancel(`Export max is ${args.exportMax}. Server limit is 10000. Reduce max or narrow filters.`);
    if (!ok) return;
  }

  args.resetUpgradeActionState();
  args.setExportLoading(true);

  try {
    const res = await args.fetchAuditExport();
    const built = args.buildAuditExportText(res);

    if (args.mode === 'copy') {
      await args.copyText(built.text);
      args.setExportInfo(built.totalCount === null
        ? `Exported ${built.returnedCount} items.`
        : `Exported ${built.returnedCount} / ${built.totalCount} items.`);
    } else {
      const fn = args.getExportFileName();
      args.downloadJson(fn, built.text);
      args.setExportInfo(built.totalCount === null
        ? `Downloaded ${built.returnedCount} items.`
        : `Downloaded ${built.returnedCount} / ${built.totalCount} items.`);
    }
  } catch (e: any) {
    args.setExportError(args.mapAuditExportError(e));
    args.setExportInfo('');
  } finally {
    args.setExportLoading(false);
  }
}
