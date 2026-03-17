export function clockDriftWarningImpl(serverNowOffsetMs: number): string {
  const abs = Math.abs(serverNowOffsetMs);
  if (abs < 120_000) return '';
  const dir = serverNowOffsetMs > 0 ? 'behind' : 'ahead of';
  const mins = Math.round(abs / 60_000);
  return `Client clock is ~${mins}m ${dir} server time. 'ago' values are server-calibrated.`;
}
