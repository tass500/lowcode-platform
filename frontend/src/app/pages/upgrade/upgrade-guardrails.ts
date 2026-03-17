export function applyEnforcementGuardrail(args: {
  isHardBlocked: () => boolean;
  isSoftBlocked: () => boolean;

  onHardBlocked: () => void;
  onSoftBlocked: () => boolean;
}): boolean {
  if (args.isHardBlocked()) {
    args.onHardBlocked();
    return false;
  }
  if (args.isSoftBlocked()) {
    const ok = args.onSoftBlocked();
    if (!ok) return false;
  }
  return true;
}
