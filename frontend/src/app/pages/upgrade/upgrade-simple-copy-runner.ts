import type { ServerBuildInfo } from './upgrade-server-build';

import { serverBuildHeaderLines } from './upgrade-server-build';

export async function copyRunIdImpl(args: {
  runId: string;
  copyText: (text: string) => Promise<void>;
}): Promise<void> {
  const id = (args.runId ?? '').trim();
  if (!id) return;
  await args.copyText(id);
}

export async function copyTraceIdImpl(args: {
  traceId: string;
  copyText: (text: string) => Promise<void>;
}): Promise<void> {
  const t = (args.traceId ?? '').trim();
  if (!t) return;
  await args.copyText(t);
}

export async function copyClientTraceHeaderArgImpl(args: {
  clientTraceId: string;
  copyText: (text: string) => Promise<void>;
}): Promise<void> {
  const t = (args.clientTraceId ?? '').trim();
  if (!t) return;
  await args.copyText(`-H 'X-Trace-Id: ${t}'`);
}

export async function copyServerHeadersImpl(args: {
  serverBuild: ServerBuildInfo | null;
  copyText: (text: string) => Promise<void>;
}): Promise<void> {
  if (!args.serverBuild) return;
  const lines = serverBuildHeaderLines(args.serverBuild);
  await args.copyText(lines.join('\n'));
}

export async function copyTriageSnapshotImpl(args: {
  text: string;
  copyText: (text: string) => Promise<void>;
}): Promise<void> {
  await args.copyText(args.text);
}
