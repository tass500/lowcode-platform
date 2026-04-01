import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type BffMetaDto = {
  enabled: boolean;
  configured: boolean;
  loginPath: string | null;
  sessionPath: string | null;
  callbackPath: string | null;
};

@Injectable({ providedIn: 'root' })
export class BffAuthStateService {
  private readonly metaSignal = signal<BffMetaDto | null>(null);

  /** Latest meta from `GET /api/auth/bff/meta` (null before first load). */
  readonly meta = this.metaSignal.asReadonly();

  /** Server-side BFF OAuth is on and OIDC client is configured — use httpOnly cookie + `withCredentials`, do not send SPA Bearer. */
  useCookieAuth(): boolean {
    const m = this.metaSignal();
    return !!m && m.enabled && m.configured;
  }

  async loadMeta(http: HttpClient): Promise<void> {
    try {
      const m = await firstValueFrom(http.get<BffMetaDto>('/api/auth/bff/meta'));
      this.metaSignal.set(m);
    } catch {
      this.metaSignal.set({
        enabled: false,
        configured: false,
        loginPath: null,
        sessionPath: null,
        callbackPath: null,
      });
    }
  }
}
