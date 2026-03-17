export function buildClientTraceHeaderArg(clientTraceId: string): string {
  const t = (clientTraceId ?? '').trim();
  if (!t) return '';
  return `-H 'X-Trace-Id: ${t}'`;
}

export function buildCurlSnippetsText(args: {
  runId: string;
  traceId: string;
  clientTraceId: string;
}): string {
  const id = (args.runId ?? '').trim();
  if (!id) return '';

  const t = (args.traceId ?? '').trim();
  const clientTrace = (args.clientTraceId ?? '').trim();
  const headerArg = clientTrace ? ` -H 'X-Trace-Id: ${clientTrace}'` : '';

  const lines: string[] = [];
  lines.push(`# Run ${id}`);
  lines.push(`curl -sS http://localhost:5002/api/admin/upgrade-runs/${id}${headerArg}`);
  lines.push(`curl -sS -X POST http://localhost:5002/api/admin/upgrade-runs/${id}/retry -H 'Content-Type: application/json'${headerArg} -d '{}'`);
  lines.push(`curl -sS -X POST http://localhost:5002/api/admin/upgrade-runs/${id}/cancel -H 'Content-Type: application/json'${headerArg} -d '{}'`);
  lines.push('');
  lines.push('# Status / observability');
  lines.push(`curl -sS http://localhost:5002/api/admin/installation/status${headerArg}`);
  lines.push(`curl -sS http://localhost:5002/api/admin/observability${headerArg}`);
  lines.push('');
  lines.push('# Queue / recent');
  lines.push(`curl -sS http://localhost:5002/api/admin/upgrade-runs/queue${headerArg}`);
  lines.push(`curl -sS "http://localhost:5002/api/admin/upgrade-runs/recent?take=10"${headerArg}`);
  lines.push(`curl -sS http://localhost:5002/api/admin/upgrade-runs/latest${headerArg}`);
  lines.push('');
  lines.push('# Audit');
  lines.push(`curl -sS "http://localhost:5002/api/admin/audit?take=50&actionContains=upgrade_"${headerArg}`);
  if (t)
    lines.push(`curl -sS "http://localhost:5002/api/admin/audit?take=50&traceId=${t}"${headerArg}`);

  return lines.join('\n');
}

export function buildAuditCurlByTraceId(args: {
  take: number;
  traceId: string;
  clientTraceId: string;
}): string {
  const t = (args.traceId ?? '').trim();
  if (!t) return '';

  const clientTrace = (args.clientTraceId ?? '').trim();
  const headerArg = clientTrace ? ` -H 'X-Trace-Id: ${clientTrace}'` : '';
  const take = args.take || 50;

  const url = `http://localhost:5002/api/admin/audit?take=${encodeURIComponent(String(take))}&traceId=${encodeURIComponent(t)}`;
  return `curl -sS "${url}"${headerArg}`;
}
