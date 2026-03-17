export function resetUpgradeRunActionErrorsImpl(args: {
  setRunError: (v: string) => void;
  setStartError: (v: string) => void;
  setDevFailError: (v: string) => void;
}): void {
  args.setRunError('');
  args.setStartError('');
  args.setDevFailError('');
}

export function resetUpgradeExportErrorsImpl(args: {
  setCopyBundleError: (v: string) => void;
  setAuditExportError: (v: string) => void;
  setAuditExportInfo: (v: string) => void;
  setDebugPackError: (v: string) => void;
}): void {
  args.setCopyBundleError('');
  args.setAuditExportError('');
  args.setAuditExportInfo('');
  args.setDebugPackError('');
}

export function resetUpgradeActionStateImpl(args: {
  resetUpgradeRunActionErrors: () => void;
  resetUpgradeExportErrors: () => void;
}): void {
  args.resetUpgradeRunActionErrors();
  args.resetUpgradeExportErrors();
}
