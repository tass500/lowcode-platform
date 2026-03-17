import {
  InstallationStatus,
  ObservabilitySnapshot,
  UpgradeRun,
} from './upgrade-types';

export function ticketHeaderPreviewImpl(args: {
  runId: string;
  run: UpgradeRun | null;
  status: InstallationStatus | null;
  observability: ObservabilitySnapshot | null;
  clientTraceId: string;
  ticketHeaderTextUnified: (a: {
    runId: string;
    run: UpgradeRun | null;
    status: InstallationStatus | null;
    observability: ObservabilitySnapshot | null;
    clientTraceId: string;
  }) => string;
}): string {
  return args.ticketHeaderTextUnified({
    runId: args.runId,
    run: args.run,
    status: args.status,
    observability: args.observability,
    clientTraceId: args.clientTraceId,
  });
}
