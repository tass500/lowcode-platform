export function clearClientTraceIdImpl(args: {
  setClientTraceId: (v: string) => void;
}): void {
  args.setClientTraceId('');
}

export function generateClientTraceIdImpl(args: {
  setClientTraceId: (v: string) => void;
}): void {
  const anyCrypto = (globalThis as any).crypto;
  if (anyCrypto?.randomUUID)
    args.setClientTraceId(anyCrypto.randomUUID());
  else
    args.setClientTraceId(`${Date.now()}-${Math.random().toString(16).slice(2)}`);
}
