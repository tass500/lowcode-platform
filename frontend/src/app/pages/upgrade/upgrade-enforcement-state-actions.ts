import type { InstallationStatus } from './upgrade-types';

export function isHardBlockedImpl(status: InstallationStatus | null | undefined): boolean {
  const s = status?.enforcementState;
  return s === 'hard_block';
}

export function isSoftBlockedImpl(status: InstallationStatus | null | undefined): boolean {
  const s = status?.enforcementState;
  return s === 'soft_block';
}
