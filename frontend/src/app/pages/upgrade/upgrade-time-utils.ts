export function parseDateUtc(v: string | null | undefined): Date | null {
  if (!v) return null;
  const dt = new Date(v);
  if (Number.isNaN(dt.getTime())) return null;
  return dt;
}

export function serverNowMs(nowTickMs: number, serverNowOffsetMs: number): number {
  return nowTickMs + serverNowOffsetMs;
}

export function serverNowDate(nowTickMs: number, serverNowOffsetMs: number): Date {
  return new Date(serverNowMs(nowTickMs, serverNowOffsetMs));
}
