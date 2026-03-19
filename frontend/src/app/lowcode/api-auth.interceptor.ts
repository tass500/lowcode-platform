import { HttpInterceptorFn } from '@angular/common/http';
import { getLowCodeSession } from './lowcode-session.store';

export const apiAuthInterceptor: HttpInterceptorFn = (req, next) => {
  // Only decorate API calls proxied to the backend.
  if (!req.url.startsWith('/api/')) {
    return next(req);
  }

  const session = getLowCodeSession();
  if (!session) {
    return next(req);
  }

  const headers: Record<string, string> = {};

  if (session.accessToken) {
    headers['Authorization'] = `Bearer ${session.accessToken}`;
  }

  // Dev helper: the backend only honors this header in Development.
  if (session.tenantSlug) {
    headers['X-Tenant-Id'] = session.tenantSlug;
  }

  return next(req.clone({ setHeaders: headers }));
};
