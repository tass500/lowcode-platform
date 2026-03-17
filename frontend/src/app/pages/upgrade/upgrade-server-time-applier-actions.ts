export function serverTimeApplierImpl(args: {
  applyServerTimeUtc: (v: string | null | undefined, source: string) => void;
}): (v: string | null | undefined, source: string) => void {
  return (v, src) => args.applyServerTimeUtc(v, src);
}
