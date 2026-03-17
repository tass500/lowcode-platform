export function curlSnippetsTextImpl(args: {
  runId: string | null | undefined;
  traceId: string | null | undefined;
  clientTraceId: string | null | undefined;
  buildCurlSnippetsText: (a: { runId: string; traceId: string; clientTraceId: string }) => string;
}): string {
  return args.buildCurlSnippetsText({
    runId: (args.runId ?? '').trim(),
    traceId: args.traceId ?? '',
    clientTraceId: (args.clientTraceId ?? '').trim(),
  });
}
