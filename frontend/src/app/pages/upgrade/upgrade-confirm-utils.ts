export function confirmOrCancel(args: {
  message: string;
}): boolean {
  return window.confirm(args.message);
}

export function confirmSoftBlockContinue(args: {
  action: 'start upgrade' | 'retry';
  daysOutOfSupport: number | null | undefined;
}): boolean {
  const days = args.daysOutOfSupport;
  const suffix = (days === 0 || !!days) ? ` (daysOutOfSupport=${days})` : '';
  return confirmOrCancel({
    message: `Installation is out of support window (soft_block). Continue ${args.action}?${suffix}`,
  });
}
