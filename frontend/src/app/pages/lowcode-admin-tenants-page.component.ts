import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

type TenantListItemDto = {
  tenantId: string;
  slug: string;
  connectionStringSecretRef?: string | null;
  connectionString?: string | null;
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

@Component({
  selector: 'app-lowcode-admin-tenants-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
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
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Created</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let t of items">
              <td style="border-bottom:1px solid #eee; padding: 6px;"><b>{{ t.slug }}</b></td>
              <td style="border-bottom:1px solid #eee; padding: 6px; font-family: monospace;">{{ t.tenantId }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ t.connectionStringSecretRef }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ t.connectionString }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ t.createdAtUtc | date: 'medium' }}</td>
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
}
