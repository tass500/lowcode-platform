import type { HttpResponse } from '@angular/common/http';

import type { ObservabilitySnapshot } from './upgrade-types';

export async function loadObservabilityImpl(args: {
  setLoading: (v: boolean) => void;
  setError: (v: string) => void;
  setObservability: (v: ObservabilitySnapshot | null) => void;
  setLastRefreshedAt: (v: Date | null) => void;

  fetch: () => Promise<HttpResponse<ObservabilitySnapshot>>;
  serverNowDate: () => Date;
  formatError: (e: any, fallback: string) => string;

  tryCaptureServerBuildFromResponse: (res: HttpResponse<ObservabilitySnapshot>) => void;
  afterSuccess: () => void;
}): Promise<void> {
  args.setLoading(true);
  args.setError('');

  try {
    const res = await args.fetch();
    args.setObservability(res.body ?? null);
    args.tryCaptureServerBuildFromResponse(res);
    args.setLastRefreshedAt(args.serverNowDate());
    args.setLoading(false);
    args.afterSuccess();
  } catch (e: any) {
    args.setObservability(null);
    args.setLoading(false);
    args.setError(args.formatError(e, 'Failed to load observability snapshot.'));
  }
}
