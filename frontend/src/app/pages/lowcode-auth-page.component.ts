import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';
import { firstValueFrom } from 'rxjs';
import { BffAuthStateService } from '../lowcode/bff-auth-state.service';
import { configureOAuthForSpa } from '../lowcode/lowcode-oidc.configure';
import type { SpaOidcConfig } from '../lowcode/lowcode-oidc.types';
import { getLowCodeSession, setLowCodeSession } from '../lowcode/lowcode-session.store';

type DevTokenResponse = {
  serverTimeUtc: string;
  accessToken: string;
  expiresAtUtc: string;
};

type BffSessionResponse = {
  authenticated: boolean;
  accessTokenExpiresAtUtc: string | null;
  tenantHint: string | null;
  subjectHint: string | null;
};

@Component({
  selector: 'app-lowcode-auth-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <main style="padding: 16px; max-width: 980px; margin: 0 auto;">
      <h2>Low-code Auth (dev)</h2>

      <div *ngIf="bffError" style="margin-top: 12px; padding: 10px; border-radius: 8px; background: #ffebee; color: #b00020;">
        BFF login error: <code>{{ bffError }}</code>
      </div>

      <section
        *ngIf="bff.meta()?.enabled"
        style="margin-top: 12px; padding: 12px; border: 1px solid #063; border-radius: 8px; background: #f0fff4;"
      >
        <h3 style="margin: 0 0 8px; font-size: 1.05rem;">Server-side login (BFF, httpOnly cookie)</h3>
        <p style="color:#444; margin: 0 0 8px;">
          Ha a backend BFF auth be van kapcsolva és az OIDC kliens be van állítva, a böngésző a JWT-t httpOnly sütiben
          tartja; az API hívások <code>withCredentials</code>-szel mennek, és nem küldünk <code>Authorization</code>-t a
          sessionStorage-ból (nehogy felülírjuk a sütit).
        </p>
        <p *ngIf="bff.meta()?.enabled && !bff.useCookieAuth()" style="color:#666; margin: 0 0 8px;">
          A szerver szerint a BFF be van kapcsolva, de hiányzik az OIDC authority + SPA client id — a BFF login nem elérhető,
          amíg ezek nincsenek beállítva.
        </p>
        <a *ngIf="bffLoginHref" [href]="bffLoginHref" style="display: inline-block; margin-right: 12px;">Belépés BFF-fel (IdP)</a>
        <ng-container *ngIf="bff.useCookieAuth()">
          <button type="button" (click)="loadBffSession()" [disabled]="bffSessionLoading">Session infó frissítése</button>
          <button type="button" (click)="bffLogout()" style="margin-left: 8px;">BFF kijelentkezés (süti törlése)</button>
          <div *ngIf="bffSessionLoading" style="margin-top: 8px;">Betöltés…</div>
          <pre
            *ngIf="bffSessionText"
            style="margin-top: 10px; padding: 8px; background: #fff; border: 1px solid #ccc; border-radius: 6px; overflow: auto;"
            >{{ bffSessionText }}</pre>
        </ng-container>
      </section>

      <section
        *ngIf="oidcCfg && !bff.useCookieAuth()"
        style="margin-top: 12px; padding: 12px; border: 1px solid #cce; border-radius: 8px; background: #f8fbff;"
      >
        <h3 style="margin: 0 0 8px; font-size: 1.05rem;">OpenID Connect (code + PKCE)</h3>
        <p style="color:#444; margin: 0 0 8px;">
          Átirányítás az IdP-re. Ha az id tokenben nincs tenant claim, töltsd ki lent a tenant mezőt, majd mentsd a
          munkamenetet (vagy kérj új dev tokent).
        </p>
        <button type="button" (click)="oidcLogin()" [disabled]="oidcLoginLoading">
          {{ oidcLoginLoading ? 'Átirányítás…' : 'Belépés OIDC-vel' }}
        </button>
      </section>

      <section style="margin-top: 12px; padding: 12px; border: 1px solid #ddd; border-radius: 8px;">
        <div *ngIf="bff.useCookieAuth()" style="margin-bottom: 10px; padding: 8px; background: #fff8e1; border-radius: 6px; color: #5d4e00;">
          BFF mód: a dev / beillesztett Bearer token <strong>nem</strong> kerül az API-kra — a süti azonosít. Tenant
          override-hoz (dev) a tenant mező továbbra is küldi az <code>X-Tenant-Id</code> fejlécet, ha kitöltöd.
        </div>
        <div style="color:#444; margin-bottom: 8px;">
          A low-code API-khoz kell token + tenant. Devben kérhetsz tokent a backendből, vagy beilleszthetsz egyet.
        </div>

        <form [formGroup]="form" (ngSubmit)="mint()" style="display:flex; gap: 12px; flex-wrap: wrap; align-items: end;">
          <label>
            Tenant slug
            <input formControlName="tenantSlug" placeholder="t1" style="min-width: 120px;" />
          </label>

          <label>
            Subject
            <input formControlName="subject" placeholder="demo-user" style="min-width: 220px;" />
          </label>

          <label>
            Roles (comma)
            <input formControlName="roles" placeholder="admin" style="min-width: 220px;" />
          </label>

          <button type="submit" [disabled]="form.invalid || mintLoading">Mint dev token</button>
          <button type="button" (click)="clear()" [disabled]="!hasSession">Clear</button>

          <div *ngIf="mintLoading">Minting...</div>
          <div *ngIf="error" style="color:#b00020;">{{ error }}</div>
        </form>

        <div style="margin-top: 12px; display:flex; gap: 12px; flex-wrap: wrap; align-items: end;">
          <label style="flex: 1; min-width: 420px;">
            Paste token
            <input [value]="pastedToken" (input)="pastedToken = (($any($event.target).value ?? '').trim())" placeholder="Bearer token" style="width: 100%;" />
          </label>
          <button type="button" (click)="savePastedToken()" [disabled]="!pastedToken || !tenantSlugValue">Save token</button>
        </div>

        <div *ngIf="hasSession" style="margin-top: 12px; font-family: monospace; color:#444;">
          <div><b>tenant</b>: {{ sessionTenant }}</div>
          <div><b>token</b>: {{ sessionTokenPreview }}</div>
        </div>
      </section>
    </main>
  `,
})
export class LowCodeAuthPageComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly oauth = inject(OAuthService);
  private readonly route = inject(ActivatedRoute);
  readonly bff = inject(BffAuthStateService);

  oidcCfg: SpaOidcConfig | null = null;
  oidcLoginLoading = false;

  bffError: string | null = null;
  bffSessionText: string | null = null;
  bffSessionLoading = false;

  form = new FormGroup({
    tenantSlug: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    subject: new FormControl('demo-user', { nonNullable: true, validators: [Validators.required] }),
    roles: new FormControl('', { nonNullable: true }),
  });

  mintedToken: string | null = null;
  pastedToken = '';
  mintLoading = false;
  error: string | null = null;

  get bffLoginHref(): string | null {
    const m = this.bff.meta();
    if (!m?.enabled || !m.loginPath?.trim()) return null;
    const p = m.loginPath.startsWith('/') ? m.loginPath : `/${m.loginPath}`;
    return p;
  }

  async ngOnInit(): Promise<void> {
    const q = this.route.snapshot.queryParamMap;
    this.bffError = q.get('bff_error') ?? q.get('Bff_error');
    try {
      this.oidcCfg = await firstValueFrom(this.http.get<SpaOidcConfig>('/api/auth/spa-oidc-config'));
    } catch {
      this.oidcCfg = null;
    }
    if (this.bff.useCookieAuth()) {
      await this.loadBffSession();
    }
  }

  async loadBffSession(): Promise<void> {
    if (!this.bff.useCookieAuth()) return;
    this.bffSessionLoading = true;
    this.bffSessionText = null;
    try {
      const s = await firstValueFrom(
        this.http.get<BffSessionResponse>('/api/auth/bff/session', { withCredentials: true })
      );
      this.bffSessionText = JSON.stringify(s, null, 2);
    } catch (e: unknown) {
      const anyErr = e as { error?: { message?: string }; message?: string };
      this.bffSessionText = anyErr?.error?.message ?? anyErr?.message ?? 'Failed to load BFF session.';
    } finally {
      this.bffSessionLoading = false;
    }
  }

  async bffLogout(): Promise<void> {
    if (!this.bff.useCookieAuth()) return;
    try {
      await firstValueFrom(this.http.post('/api/auth/bff/logout', {}, { withCredentials: true }));
    } catch {
      /* still clear local hints */
    }
    setLowCodeSession(null);
    this.bffSessionText = null;
    await this.loadBffSession();
  }

  async oidcLogin(): Promise<void> {
    if (!this.oidcCfg) return;
    this.oidcLoginLoading = true;
    this.error = null;
    try {
      configureOAuthForSpa(this.oauth, this.oidcCfg);
      await this.oauth.loadDiscoveryDocumentAndLogin();
    } catch (e: unknown) {
      const anyErr = e as { error?: { message?: string }; message?: string };
      this.error = anyErr?.error?.message ?? anyErr?.message ?? 'OIDC login failed.';
    } finally {
      this.oidcLoginLoading = false;
    }
  }

  get hasSession(): boolean {
    return !!getLowCodeSession();
  }

  get tenantSlugValue(): string {
    return (this.form.controls.tenantSlug.value ?? '').trim();
  }

  get sessionTenant(): string {
    return getLowCodeSession()?.tenantSlug ?? '';
  }

  get sessionTokenPreview(): string {
    const t = getLowCodeSession()?.accessToken ?? '';
    if (!t) return '';
    return t.length <= 24 ? t : `${t.substring(0, 12)}...${t.substring(t.length - 12)}`;
  }

  async mint(): Promise<void> {
    this.error = null;
    this.mintLoading = true;

    try {
      const rolesRaw = (this.form.controls.roles.value ?? '').trim();
      const roles = rolesRaw
        ? rolesRaw.split(',').map(x => x.trim()).filter(Boolean)
        : [];

      const req = {
        subject: (this.form.controls.subject.value ?? '').trim(),
        tenantSlug: this.tenantSlugValue,
        roles,
      };

      const res = await firstValueFrom(this.http.post<DevTokenResponse>('/api/auth/dev-token', req));
      setLowCodeSession({ tenantSlug: this.tenantSlugValue, accessToken: res.accessToken });

      this.mintedToken = res.accessToken;
      this.pastedToken = '';
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to mint token.';
    } finally {
      this.mintLoading = false;
    }
  }

  savePastedToken(): void {
    this.error = null;
    const t = (this.pastedToken ?? '').trim();
    if (!t) return;

    setLowCodeSession({ tenantSlug: this.tenantSlugValue, accessToken: t });
    this.mintedToken = null;
    this.pastedToken = '';
  }

  clear(): void {
    setLowCodeSession(null);
    this.mintedToken = null;
    this.pastedToken = '';
    this.error = null;
  }
}
