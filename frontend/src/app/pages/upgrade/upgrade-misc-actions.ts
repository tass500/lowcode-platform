export function onAuditFilterChangedImpl(args: {
  setAuditSkip: (v: number) => void;
  setAuditError: (v: string) => void;
  setAuditExportError: (v: string) => void;
  setAuditExportInfo: (v: string) => void;
}): void {
  args.setAuditSkip(0);
  args.setAuditError('');
  args.setAuditExportError('');
  args.setAuditExportInfo('');
}

export function downloadIncidentBundleImpl(args: {
  runIncidentBundleExport: (mode: 'copy' | 'download') => Promise<void>;
}): void {
  void args.runIncidentBundleExport('download');
}

export function copyDebugPackIndexImpl(args: {
  runDebugPack: (mode: 'copy' | 'download') => Promise<void>;
}): void {
  void args.runDebugPack('copy');
}

export async function downloadDebugPackIndexImpl(args: {
  runDebugPack: (mode: 'copy' | 'download') => Promise<void>;
}): Promise<void> {
  await args.runDebugPack('download');
}

export async function copyIncidentBundleImpl(args: {
  runIncidentBundleExport: (mode: 'copy' | 'download') => Promise<void>;
}): Promise<void> {
  await args.runIncidentBundleExport('copy');
}
