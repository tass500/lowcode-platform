export type DevPreset = 'ok' | 'warn' | 'soft_block' | 'hard_block';

export function applyDevPresetImpl(args: {
  preset: DevPreset;
  patchValue: (v: { currentVersion: string; supportedVersion: string; upgradeWindowDays: number }) => void;
}): void {
  if (args.preset === 'ok') {
    args.patchValue({ currentVersion: '0.1.0', supportedVersion: '0.1.0', upgradeWindowDays: 60 });
    return;
  }
  if (args.preset === 'warn') {
    args.patchValue({ currentVersion: '0.1.0', supportedVersion: '0.2.0', upgradeWindowDays: 60 });
    return;
  }
  if (args.preset === 'soft_block') {
    args.patchValue({ currentVersion: '0.1.0', supportedVersion: '0.3.0', upgradeWindowDays: 60 });
    return;
  }

  args.patchValue({ currentVersion: '0.1.0', supportedVersion: '1.0.0', upgradeWindowDays: 60 });
}
