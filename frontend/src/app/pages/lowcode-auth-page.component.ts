import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { OAuthService } from 'angular-oauth2-oidc';
import { firstValueFrom } from 'rxjs';
import { configureOAuthForSpa } from '../lowcode/lowcode-oidc.configure';
import type { SpaOidcConfig } from '../lowcode/lowcode-oidc.types';
import { getLowCodeSession, setLowCodeSession } from '../lowcode/lowcode-session.store';

type DevTokenResponse = {
  serverTimeUtc: string;
  accessToken: string;
  expiresAtUtc: string;
};

@Component({
  selector: 'app-lowcode-auth-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <main style="padding: 16px; max-width: 980px; margin: 0 auto;">
      <h2>Low-code Auth (dev)</h2>

      <section
        *ngIf="oidcCfg"
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

  oidcCfg: SpaOidcConfig | null = null;
  oidcLoginLoading = false;

  form = new FormGroup({
    tenantSlug: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    subject: new FormControl('demo-user', { nonNullable: true, validators: [Validators.required] }),
    roles: new FormControl('', { nonNullable: true }),
  });

  mintedToken: string | null = null;
  pastedToken = '';
  mintLoading = false;
  error: string | null = null;

  async ngOnInit(): Promise<void> {
    try {
      this.oidcCfg = await firstValueFrom(this.http.get<SpaOidcConfig>('/api/auth/spa-oidc-config'));
    } catch {
      this.oidcCfg = null;
    }
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
