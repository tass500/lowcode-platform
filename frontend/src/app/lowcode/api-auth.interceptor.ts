import { HttpClient, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, map, of, switchMap } from 'rxjs';
import { BffAuthStateService } from './bff-auth-state.service';
import { decodeJwtExpSec } from './jwt-exp';
import { getLowCodeSession, setLowCodeSession, type LowCodeSession } from './lowcode-session.store';

function needsOidcRefresh(session: LowCodeSession): boolean {
  if (!session.refreshToken || !session.oidcTokenEndpoint || !session.oidcClientId) return false;
  const exp = decodeJwtExpSec(session.accessToken);
  if (exp === null) return true;
  return exp * 1000 <= Date.now() + 60_000;
}

function withCookieAuth(req: HttpRequest<unknown>): HttpRequest<unknown> {
  const session = getLowCodeSession();
  if (!session?.tenantSlug) {
    return req.clone({ withCredentials: true });
  }
  return req.clone({ withCredentials: true, setHeaders: { 'X-Tenant-Id': session.tenantSlug } });
}

export const apiAuthInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/')) {
    return next(req);
  }

  const bff = inject(BffAuthStateService);
  if (bff.useCookieAuth()) {
    return next(withCookieAuth(req));
  }

  const http = inject(HttpClient);
  const session = getLowCodeSession();
  if (!session) {
    return next(req);
  }

  if (!needsOidcRefresh(session)) {
    return next(withSessionHeaders(req, session));
  }

  const body = new URLSearchParams({
    grant_type: 'refresh_token',
    refresh_token: session.refreshToken!,
    client_id: session.oidcClientId!,
  });

  return http
    .post<{ access_token?: string; refresh_token?: string }>(session.oidcTokenEndpoint!, body.toString(), {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    })
    .pipe(
      map(resp => {
        const access = resp.access_token;
        if (!access) return session;
        const nextSession: LowCodeSession = {
          ...session,
          accessToken: access,
          refreshToken: resp.refresh_token ?? session.refreshToken,
        };
        setLowCodeSession(nextSession);
        return nextSession;
      }),
      catchError(() => of(session)),
      switchMap(s => next(withSessionHeaders(req, s)))
    );
};

function withSessionHeaders(req: HttpRequest<unknown>, session: LowCodeSession) {
  const headers: Record<string, string> = {};
  if (session.accessToken) {
    headers['Authorization'] = `Bearer ${session.accessToken}`;
  }
  if (session.tenantSlug) {
    headers['X-Tenant-Id'] = session.tenantSlug;
  }
  return req.clone({ setHeaders: headers });
}
