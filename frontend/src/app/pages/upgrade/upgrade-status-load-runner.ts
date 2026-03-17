import type { HttpResponse } from '@angular/common/http';

import type { InstallationStatus } from './upgrade-types';

export async function loadStatusImpl(args: {
  setLoading: (v: boolean) => void;
  setError: (v: string) => void;
  setStatus: (v: InstallationStatus | null) => void;

  fetch: () => Promise<HttpResponse<InstallationStatus>>;
  formatError: (e: any, fallback: string) => string;

  tryCaptureServerBuildFromResponse: (res: HttpResponse<InstallationStatus>) => void;
}): Promise<void> {
  args.setLoading(true);
  args.setError('');

  try {
    const res = await args.fetch();
    args.setStatus(res.body ?? null);
    args.tryCaptureServerBuildFromResponse(res);
    args.setLoading(false);
  } catch (e: any) {
    args.setStatus(null);
    args.setLoading(false);
    args.setError(args.formatError(e, 'Failed to load status.'));
  }
}
