import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

type TenantListItemDto = {
  tenantId: string;
  slug: string;
  connectionStringSecretRef?: string | null;
  connectionString?: string | null;
  tenantApiKeyConfigured?: boolean;
  createdAtUtc: string;
};

type TenantListResponse = {
  serverTimeUtc: string;
  items: TenantListItemDto[];
};

type TenantMigrationResult = {
  tenantSlug: string;
  succeeded: boolean;
  error?: string | null;
  startedAtUtc: string;
  finishedAtUtc: string;
};

type CreateTenantResponse = {
  tenantId: string;
  slug: string;
  connectionStringSecretRef?: string | null;
  migration: TenantMigrationResult;
};

type TenantApiKeyProvisionedResponse = {
  serverTimeUtc: string;
  apiKey: string;
};

@Component({
  selector: 'app-lowcode-admin-tenants-page',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
    <main style="padding: 16px; max-width: 980px; margin: 0 auto;">
      <h2 style="margin:0;">Admin / Tenants</h2>

      <section style="margin-top: 12px; padding: 12px; border: 1px solid #ddd; border-radius: 8px;">
        <div style="display:flex; gap: 12px; flex-wrap: wrap; align-items: center;">
          <button type="button" (click)="load()" [disabled]="loading">Refresh</button>
          <button type="button" (click)="migrateAll()" [disabled]="migrating">Migrate all tenant DBs</button>
          <div *ngIf="loading">Loading...</div>
          <div *ngIf="migrating">Migrating...</div>
          <div *ngIf="error" style="color:#b00020;">{{ error }}</div>
        </div>

        <div
          *ngIf="revealedApiKey"
          style="margin-top: 12px; padding: 10px 12px; border: 1px solid #c9daf8; border-radius: 8px; background: #f3f7ff;"
        >
          <div style="font-weight: 600; margin-bottom: 6px;">New tenant API key (copy now — not shown again)</div>
          <div style="font-size: 12px; color:#555; margin-bottom: 6px;">Tenant: <code>{{ revealedApiKey.slug }}</code></div>
          <div style="display:flex; flex-wrap: wrap; gap: 8px; align-items: center;">
            <code style="word-break: break-all; flex: 1; min-width: 200px;">{{ revealedApiKey.key }}</code>
            <button type="button" (click)="copyRevealedKey()">Copy</button>
            <button type="button" (click)="dismissRevealedKey()">Dismiss</button>
          </div>
          <div style="margin-top: 8px; font-size: 12px; color:#444;">
            Use header <code>X-Tenant-Api-Key</code> on tenant API calls. Details: repository file
            <code>docs/live/tenant-api-key.md</code>.
          </div>
        </div>

        <div *ngIf="migrationResults.length" style="margin-top: 12px;">
          <div style="font-weight: 600; margin-bottom: 6px;">Migration results</div>
          <table style="width:100%; border-collapse: collapse;">
            <thead>
              <tr>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Tenant</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Succeeded</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Error</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let r of migrationResults">
                <td style="border-bottom:1px solid #eee; padding: 6px; font-family: monospace;">{{ r.tenantSlug }}</td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">{{ r.succeeded }}</td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">{{ r.error }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div style="margin-top: 14px; font-weight: 600;">Create tenant</div>

        <form [formGroup]="form" (ngSubmit)="create()" style="margin-top: 8px; display:flex; gap: 12px; flex-wrap: wrap; align-items: end;">
          <label>
            Slug
            <input formControlName="slug" placeholder="t2" style="min-width: 160px;" />
          </label>

          <label>
            ConnectionStringSecretRef
            <input formControlName="connectionStringSecretRef" placeholder="t2" style="min-width: 220px;" />
          </label>

          <label>
            ConnectionString
            <input formControlName="connectionString" placeholder="Data Source=tenant-t2.db" style="min-width: 320px;" />
          </label>

          <button type="submit" [disabled]="form.invalid || creating">Create</button>
          <div *ngIf="creating">Creating...</div>
        </form>

        <div style="margin-top: 14px; font-weight: 600;">Tenants</div>

        <table *ngIf="items.length" style="width:100%; border-collapse: collapse; margin-top: 8px;">
          <thead>
            <tr>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Slug</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">TenantId</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">SecretRef</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">ConnectionString</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">API key</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Created</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let t of items">
              <td style="border-bottom:1px solid #eee; padding: 6px;"><b>{{ t.slug }}</b></td>
              <td style="border-bottom:1px solid #eee; padding: 6px; font-family: monospace;">{{ t.tenantId }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ t.connectionStringSecretRef }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ t.connectionString }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">
                <span *ngIf="t.tenantApiKeyConfigured" style="color:#0a6;">configured</span>
                <span *ngIf="!t.tenantApiKeyConfigured" style="color:#888;">—</span>
              </td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ t.createdAtUtc | date: 'medium' }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px; vertical-align: top;">
                <div style="display:flex; flex-direction: column; gap: 6px; align-items: flex-start;">
                  <div style="display:flex; flex-wrap: wrap; gap: 6px;">
                    <button
                      type="button"
                      (click)="provisionRandomTenantApiKey(t.slug)"
                      [disabled]="apiKeyBusySlug === t.slug"
                    >
                      New random key
                    </button>
                    <button type="button" (click)="toggleCustomApiKey(t.slug)">
                      {{ expandedCustomApiKeySlug === t.slug ? 'Hide custom' : 'Custom key…' }}
                    </button>
                    <button
                      type="button"
                      (click)="removeTenantApiKey(t.slug)"
                      [disabled]="!t.tenantApiKeyConfigured || apiKeyBusySlug === t.slug"
                      style="color:#b00020;"
                    >
                      Remove
                    </button>
                  </div>
                  <div *ngIf="expandedCustomApiKeySlug === t.slug" style="display:flex; flex-wrap: wrap; gap: 6px; align-items: center;">
                    <input
                      type="password"
                      [(ngModel)]="customTenantApiKeyDraft"
                      placeholder="min. 24 characters"
                      autocomplete="off"
                      style="min-width: 200px; font-family: monospace;"
                    />
                    <button
                      type="button"
                      (click)="provisionCustomTenantApiKey(t.slug)"
                      [disabled]="apiKeyBusySlug === t.slug"
                    >
                      Set custom key
                    </button>
                  </div>
                </div>
              </td>
            </tr>
          </tbody>
        </table>

        <div *ngIf="!items.length" style="margin-top: 8px; color:#444;">No tenants.</div>
      </section>
    </main>
  `,
})
export class LowCodeAdminTenantsPageComponent implements OnInit {
  private readonly http = inject(HttpClient);

  items: TenantListItemDto[] = [];
  migrationResults: TenantMigrationResult[] = [];

  /** Shown once after POST tenant-api-key succeeds. */
  revealedApiKey: { slug: string; key: string } | null = null;
  apiKeyBusySlug: string | null = null;
  expandedCustomApiKeySlug: string | null = null;
  customTenantApiKeyDraft = '';

  loading = false;
  creating = false;
  migrating = false;
  error: string | null = null;

  form = new FormGroup({
    slug: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    connectionStringSecretRef: new FormControl('', { nonNullable: true }),
    connectionString: new FormControl('', { nonNullable: true }),
  });

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const res = await firstValueFrom(this.http.get<TenantListResponse>('/api/admin/tenants'));
      this.items = res.items ?? [];
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to load tenants.';
      this.items = [];
    } finally {
      this.loading = false;
    }
  }

  async create(): Promise<void> {
    this.creating = true;
    this.error = null;

    try {
      const slug = this.form.controls.slug.value.trim();
      const connectionStringSecretRefRaw = this.form.controls.connectionStringSecretRef.value.trim();
      const connectionStringRaw = this.form.controls.connectionString.value.trim();

      const req = {
        slug,
        connectionStringSecretRef: connectionStringSecretRefRaw || null,
        connectionString: connectionStringRaw || null,
      };

      const res = await firstValueFrom(this.http.post<CreateTenantResponse>('/api/admin/tenants', req));
      this.migrationResults = [res.migration];

      await this.load();
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to create tenant.';
    } finally {
      this.creating = false;
    }
  }

  async migrateAll(): Promise<void> {
    this.migrating = true;
    this.error = null;

    try {
      const res = await firstValueFrom(this.http.post<TenantMigrationResult[]>('/api/admin/tenants/migrate', {}));
      this.migrationResults = res ?? [];
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to migrate tenants.';
    } finally {
      this.migrating = false;
    }
  }

  dismissRevealedKey(): void {
    this.revealedApiKey = null;
  }

  async copyRevealedKey(): Promise<void> {
    if (!this.revealedApiKey) return;
    try {
      await navigator.clipboard.writeText(this.revealedApiKey.key);
    } catch {
      this.error = 'Clipboard copy failed (browser permission).';
    }
  }

  toggleCustomApiKey(slug: string): void {
    if (this.expandedCustomApiKeySlug === slug) {
      this.expandedCustomApiKeySlug = null;
      this.customTenantApiKeyDraft = '';
    } else {
      this.expandedCustomApiKeySlug = slug;
      this.customTenantApiKeyDraft = '';
    }
  }

  async provisionRandomTenantApiKey(slug: string): Promise<void> {
    this.apiKeyBusySlug = slug;
    this.error = null;
    try {
      const res = await firstValueFrom(
        this.http.post<TenantApiKeyProvisionedResponse>(`/api/admin/tenants/${encodeURIComponent(slug)}/tenant-api-key`, {}),
      );
      this.revealedApiKey = { slug, key: res.apiKey };
      this.expandedCustomApiKeySlug = null;
      this.customTenantApiKeyDraft = '';
      await this.load();
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to provision tenant API key.';
    } finally {
      this.apiKeyBusySlug = null;
    }
  }

  async provisionCustomTenantApiKey(slug: string): Promise<void> {
    const key = this.customTenantApiKeyDraft.trim();
    if (key.length < 24) {
      this.error = 'Custom API key must be at least 24 characters.';
      return;
    }
    this.apiKeyBusySlug = slug;
    this.error = null;
    try {
      const res = await firstValueFrom(
        this.http.post<TenantApiKeyProvisionedResponse>(`/api/admin/tenants/${encodeURIComponent(slug)}/tenant-api-key`, {
          apiKey: key,
        }),
      );
      this.revealedApiKey = { slug, key: res.apiKey };
      this.customTenantApiKeyDraft = '';
      this.expandedCustomApiKeySlug = null;
      await this.load();
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to set custom tenant API key.';
    } finally {
      this.apiKeyBusySlug = null;
    }
  }

  async removeTenantApiKey(slug: string): Promise<void> {
    if (!confirm(`Remove tenant API key for "${slug}"? Automation using this key will stop working.`)) {
      return;
    }
    this.apiKeyBusySlug = slug;
    this.error = null;
    try {
      await firstValueFrom(this.http.delete(`/api/admin/tenants/${encodeURIComponent(slug)}/tenant-api-key`));
      this.revealedApiKey = null;
      await this.load();
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to remove tenant API key.';
    } finally {
      this.apiKeyBusySlug = null;
    }
  }
}
