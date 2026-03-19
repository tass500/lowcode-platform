import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

type EntityDefinitionDetailsDto = {
  entityDefinitionId: string;
  name: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  fields: Array<unknown>;
};

type EntityRecordListItemDto = {
  entityRecordId: string;
  entityDefinitionId: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  dataJson: string;
};

type EntityRecordListResponse = {
  serverTimeUtc: string;
  items: EntityRecordListItemDto[];
};

@Component({
  selector: 'app-lowcode-entity-records-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <main style="padding: 16px; max-width: 980px; margin: 0 auto;">
      <div style="display:flex; gap: 12px; align-items: center; flex-wrap: wrap;">
        <a routerLink="/lowcode/entities">← Entities</a>
        <a *ngIf="entity" [routerLink]="['/lowcode/entities', entity.entityDefinitionId]">← Entity</a>
        <h2 style="margin:0;">Entity Records</h2>
        <button type="button" (click)="load()" [disabled]="loading">Refresh</button>
        <div *ngIf="loading">Loading...</div>
        <div *ngIf="error" style="color:#b00020;">{{ error }}</div>
      </div>

      <section *ngIf="entity" style="margin-top: 12px;">
        <div style="display:flex; gap: 24px; flex-wrap: wrap; color:#444;">
          <div><b>Entity</b>: {{ entity.name }}</div>
          <div style="font-family: monospace;"><b>Id</b>: {{ entity.entityDefinitionId }}</div>
        </div>

        <h3 style="margin-top: 18px;">Create record</h3>

        <div style="display:flex; gap: 12px; flex-wrap: wrap; align-items: end;">
          <label style="flex: 1 1 520px;">
            JSON
            <textarea [value]="recordDraftJson" (input)="recordDraftJson = $any($event.target).value" rows="7" style="width: 100%; font-family: monospace;"></textarea>
          </label>
          <button type="button" (click)="createRecord()" [disabled]="creating">Create</button>
          <div *ngIf="creating">Creating...</div>
        </div>

        <div *ngIf="createError" style="margin-top: 8px; color:#b00020;">{{ createError }}</div>

        <h3 style="margin-top: 18px;">Records</h3>

        <div style="display:flex; gap: 12px; flex-wrap: wrap; align-items: end;">
          <label>
            Filter
            <input [value]="filterText" (input)="filterText = $any($event.target).value" placeholder="search in JSON" style="min-width: 260px;" />
          </label>
        </div>

        <div *ngIf="recordsError" style="margin-top: 8px; color:#b00020;">{{ recordsError }}</div>

        <table *ngIf="filteredRecords.length" style="width:100%; border-collapse: collapse; margin-top: 12px;">
          <thead>
            <tr>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Updated</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Id</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Data</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;"></th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let r of filteredRecords">
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ r.updatedAtUtc | date: 'medium' }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px; font-family: monospace;">{{ r.entityRecordId }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">
                <textarea [value]="recordEdits[r.entityRecordId] !== undefined ? recordEdits[r.entityRecordId] : r.dataJson"
                          (input)="recordEdits[r.entityRecordId] = $any($event.target).value"
                          rows="7"
                          style="width: 100%; font-family: monospace;"></textarea>
              </td>
              <td style="border-bottom:1px solid #eee; padding: 6px; white-space: nowrap;">
                <button type="button" (click)="saveRecord(r.entityRecordId)" [disabled]="saving[r.entityRecordId]">Save</button>
                <button type="button" (click)="deleteRecord(r.entityRecordId)" [disabled]="deleting[r.entityRecordId]">Delete</button>
              </td>
            </tr>
          </tbody>
        </table>

        <div *ngIf="!filteredRecords.length" style="margin-top: 8px; color:#444;">No records.</div>
      </section>
    </main>
  `,
})
export class LowCodeEntityRecordsPageComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);

  entity: EntityDefinitionDetailsDto | null = null;

  loading = false;
  error: string | null = null;

  records: EntityRecordListItemDto[] = [];
  recordsError: string | null = null;

  filterText = '';

  recordEdits: Record<string, string> = {};
  saving: Record<string, boolean> = {};
  deleting: Record<string, boolean> = {};

  recordDraftJson = '{"name":"Acme Ltd","taxNumber":"123","riskScore":10,"status":"active"}';
  creating = false;
  createError: string | null = null;

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  private get entityId(): string {
    const raw = this.route.snapshot.paramMap.get('id');
    return String(raw === null || raw === undefined ? '' : raw).trim();
  }

  get filteredRecords(): EntityRecordListItemDto[] {
    const q = String(this.filterText ?? '').trim().toLowerCase();
    if (!q) return this.records;
    return this.records.filter(r => (r.dataJson ?? '').toLowerCase().includes(q));
  }

  private isValidJsonObject(value: string): boolean {
    try {
      const parsed = JSON.parse(value);
      return parsed && typeof parsed === 'object' && !Array.isArray(parsed);
    } catch {
      return false;
    }
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = null;
    this.recordsError = null;
    this.createError = null;

    try {
      const id = this.entityId;
      if (!id) {
        this.error = 'Missing entity id.';
        this.entity = null;
        this.records = [];
        return;
      }

      this.entity = await firstValueFrom(this.http.get<EntityDefinitionDetailsDto>(`/api/entities/${id}`));
      await this.loadRecords();
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to load entity records.';
      this.entity = null;
      this.records = [];
    } finally {
      this.loading = false;
    }
  }

  private async loadRecords(): Promise<void> {
    if (!this.entity) {
      this.records = [];
      return;
    }

    try {
      const res = await firstValueFrom(
        this.http.get<EntityRecordListResponse>(`/api/entities/${this.entity.entityDefinitionId}/records`)
      );

      this.records = res.items ?? [];

      const nextEdits: Record<string, string> = {};
      for (const r of this.records) {
        if (this.recordEdits[r.entityRecordId] !== undefined) nextEdits[r.entityRecordId] = this.recordEdits[r.entityRecordId];
      }
      this.recordEdits = nextEdits;
    } catch (e: any) {
      this.recordsError = e?.error?.message ?? e?.message ?? 'Failed to load records.';
      this.records = [];
    }
  }

  async createRecord(): Promise<void> {
    if (!this.entity) return;

    this.creating = true;
    this.createError = null;

    try {
      const dataJson = String(this.recordDraftJson ?? '').trim();
      if (!this.isValidJsonObject(dataJson)) {
        this.createError = 'Record JSON must be a JSON object.';
        return;
      }

      await firstValueFrom(
        this.http.post(`/api/entities/${this.entity.entityDefinitionId}/records`, {
          dataJson,
        })
      );

      await this.loadRecords();
    } catch (e: any) {
      this.createError = e?.error?.message ?? e?.message ?? 'Failed to create record.';
    } finally {
      this.creating = false;
    }
  }

  async saveRecord(recordId: string): Promise<void> {
    if (!this.entity) return;

    this.saving[recordId] = true;
    this.recordsError = null;

    try {
      const current = this.records.find(x => x.entityRecordId === recordId);
      if (!current) throw new Error('Record not found in UI state.');

      const candidate = this.recordEdits[recordId] !== undefined ? this.recordEdits[recordId] : current.dataJson;
      const dataJson = String(candidate ?? '').trim();
      if (!this.isValidJsonObject(dataJson)) {
        this.recordsError = 'Record JSON must be a JSON object.';
        return;
      }

      await firstValueFrom(
        this.http.put(`/api/entities/${this.entity.entityDefinitionId}/records/${recordId}`, {
          dataJson,
        })
      );

      delete this.recordEdits[recordId];
      await this.loadRecords();
    } catch (e: any) {
      this.recordsError = e?.error?.message ?? e?.message ?? 'Failed to save record.';
    } finally {
      this.saving[recordId] = false;
    }
  }

  async deleteRecord(recordId: string): Promise<void> {
    if (!this.entity) return;

    this.deleting[recordId] = true;
    this.recordsError = null;

    try {
      await firstValueFrom(this.http.delete(`/api/entities/${this.entity.entityDefinitionId}/records/${recordId}`));
      delete this.recordEdits[recordId];
      await this.loadRecords();
    } catch (e: any) {
      this.recordsError = e?.error?.message ?? e?.message ?? 'Failed to delete record.';
    } finally {
      this.deleting[recordId] = false;
    }
  }
}
