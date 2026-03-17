export function findErrorDetailMessageImpl(e: any, path: string): string {
  const details = e?.error?.details;
  if (!Array.isArray(details)) return '';
  for (const d of details) {
    if (d?.path === path && typeof d?.message === 'string')
      return (d.message ?? '').trim();
  }
  return '';
}

export function mapEnforcementBlockErrorImpl(args: {
  e: any;
  formatError: (e: any, fallback: string) => string;
}): string {
  const base = args.formatError(args.e, 'Upgrade operations are blocked until the installation is upgraded.').trim();
  const code = (args.e?.error?.errorCode ?? '').trim();
  if (code !== 'supported_version_enforcement_block')
    return base;

  const st = findErrorDetailMessageImpl(args.e, 'enforcementState');
  const days = findErrorDetailMessageImpl(args.e, 'daysOutOfSupport');

  const extraParts: string[] = [];
  if (st) extraParts.push(`enforcementState=${st}`);
  if (days) extraParts.push(`daysOutOfSupport=${days}`);
  if (extraParts.length === 0)
    return base;
  return `${base} (${extraParts.join(', ')})`;
}
