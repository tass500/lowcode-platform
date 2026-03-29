import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';
import { firstValueFrom } from 'rxjs';
import { configureOAuthForSpa } from '../lowcode/lowcode-oidc.configure';
import type { SpaOidcConfig } from '../lowcode/lowcode-oidc.types';
import { pickTenantFromClaims } from '../lowcode/lowcode-oidc-tenant-claims';
import { setLowCodeSession } from '../lowcode/lowcode-session.store';

@Component({
  selector: 'app-lowcode-auth-callback',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <main style="padding: 16px; max-width: 720px; margin: 0 auto;">
      <p *ngIf="!error">Bejelentkezés…</p>
      <p *ngIf="error" style="color:#b00020;">{{ error }}</p>
      <p style="margin-top: 12px;"><a routerLink="/lowcode/auth">Vissza az auth oldalra</a></p>
    </main>
  `,
})
export class LowcodeAuthCallbackComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly oauth = inject(OAuthService);
  private readonly router = inject(Router);

  error: string | null = null;

  async ngOnInit(): Promise<void> {
    try {
      const cfg = await firstValueFrom(this.http.get<SpaOidcConfig>('/api/auth/spa-oidc-config'));
      configureOAuthForSpa(this.oauth, cfg);
      const ok = await this.oauth.loadDiscoveryDocumentAndTryLogin();
      if (!ok || !this.oauth.hasValidAccessToken()) {
        this.error = 'A bejelentkezés nem sikerült (token hiányzik).';
        return;
      }
      const claims = this.oauth.getIdentityClaims() as Record<string, unknown> | null;
      const tenant = pickTenantFromClaims(claims, cfg.tenantClaimSources);
      const tokenEndpoint = this.oauth.tokenEndpoint;
      if (!tokenEndpoint) {
        this.error = 'OIDC discovery: token endpoint hiányzik.';
        return;
      }
      setLowCodeSession({
        tenantSlug: tenant ?? '',
        accessToken: this.oauth.getAccessToken(),
        refreshToken: this.oauth.getRefreshToken() ?? undefined,
        oidcTokenEndpoint: tokenEndpoint,
        oidcClientId: cfg.clientId,
      });
      await this.router.navigateByUrl('/lowcode/workflows');
    } catch (e: unknown) {
      if (e instanceof HttpErrorResponse && e.status === 404) {
        this.error = 'OIDC SPA config nem érhető el (404).';
        return;
      }
      const anyErr = e as { error?: { message?: string }; message?: string };
      this.error = anyErr?.error?.message ?? anyErr?.message ?? 'Ismeretlen hiba.';
    }
  }
}
