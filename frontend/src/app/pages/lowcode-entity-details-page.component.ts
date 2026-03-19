import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

type FieldDefinitionListItemDto = {
  fieldDefinitionId: string;
  entityDefinitionId: string;
  name: string;
  fieldType: string;
  isRequired: boolean;
  maxLength?: number | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

type EntityDefinitionDetailsDto = {
  entityDefinitionId: string;
  name: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  fields: FieldDefinitionListItemDto[];
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

type FieldEdit = {
  name: string;
  fieldType: string;
  isRequired: boolean;
  maxLength: string;
};

@Component({
  selector: 'app-lowcode-entity-details-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <main style="padding: 16px; max-width: 980px; margin: 0 auto;">
      <div style="display:flex; gap: 12px; align-items: center; flex-wrap: wrap;">
        <a routerLink="/lowcode/entities">← Entities</a>
        <h2 style="margin:0;">Entity</h2>
        <button type="button" (click)="load()" [disabled]="loading">Refresh</button>
        <a *ngIf="entity" [routerLink]="['/lowcode/entities', entity.entityDefinitionId, 'records']">Records</a>
        <button type="button" (click)="saveEntity()" [disabled]="!entity || entityForm.invalid || savingEntity">Save</button>
        <button type="button" (click)="deleteEntity()" [disabled]="!entity || deletingEntity">Delete</button>
        <div *ngIf="loading">Loading...</div>
        <div *ngIf="savingEntity">Saving...</div>
        <div *ngIf="deletingEntity">Deleting...</div>
        <div *ngIf="error" style="color:#b00020;">{{ error }}</div>
      </div>

      <section *ngIf="entity" style="margin-top: 12px;">
        <div style="display:flex; gap: 24px; flex-wrap: wrap; color:#444;">
          <div style="font-family: monospace;"><b>Id</b>: {{ entity.entityDefinitionId }}</div>
          <div><b>Updated</b>: {{ entity.updatedAtUtc | date: 'medium' }}</div>
        </div>

        <form [formGroup]="entityForm" style="margin-top: 12px; display:flex; flex-direction: column; gap: 12px;">
          <label>
            Name
            <input formControlName="name" style="width: 100%; max-width: 520px;" />
          </label>
        </form>

        <h3 style="margin-top: 18px;">Fields</h3>

        <div style="display:flex; gap: 12px; flex-wrap: wrap; align-items: end;">
          <label>
            Name
            <input [value]="fieldDraft.name" (input)="fieldDraft.name = $any($event.target).value" placeholder="taxNumber" />
          </label>
          <label>
            Type
            <input [value]="fieldDraft.fieldType" (input)="fieldDraft.fieldType = $any($event.target).value" placeholder="string" />
          </label>
          <label>
            Required
            <input type="checkbox" [checked]="fieldDraft.isRequired" (change)="fieldDraft.isRequired = !!$any($event.target).checked" />
          </label>
          <label>
            MaxLength
            <input [value]="fieldDraft.maxLengthText" (input)="fieldDraft.maxLengthText = $any($event.target).value" placeholder="320" style="width: 120px;" />
          </label>
          <button type="button" (click)="createField()" [disabled]="creatingField || !entity">Add field</button>
          <div *ngIf="creatingField">Adding...</div>
        </div>

        <div *ngIf="fieldError" style="margin-top: 8px; color:#b00020;">{{ fieldError }}</div>

        <table *ngIf="entity.fields?.length" style="width:100%; border-collapse: collapse; margin-top: 12px;">
          <thead>
            <tr>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Name</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Type</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Required</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">MaxLength</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;"></th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let f of entity.fields">
              <td style="border-bottom:1px solid #eee; padding: 6px;">
                <input [value]="f.name" (input)="setFieldEdit(f.fieldDefinitionId, 'name', $any($event.target).value)" />
              </td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">
                <input [value]="f.fieldType" (input)="setFieldEdit(f.fieldDefinitionId, 'fieldType', $any($event.target).value)" />
              </td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">
                <input type="checkbox" [checked]="f.isRequired" (change)="setFieldEdit(f.fieldDefinitionId, 'isRequired', !!$any($event.target).checked)" />
              </td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">
                <input [value]="f.maxLength !== null && f.maxLength !== undefined ? f.maxLength : ''" (input)="setFieldEdit(f.fieldDefinitionId, 'maxLength', $any($event.target).value)" style="width: 120px;" />
              </td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">
                <button type="button" (click)="saveField(f.fieldDefinitionId)" [disabled]="fieldSaving[f.fieldDefinitionId]">Save</button>
                <button type="button" (click)="deleteField(f.fieldDefinitionId)" [disabled]="fieldDeleting[f.fieldDefinitionId]">Delete</button>
              </td>
            </tr>
          </tbody>
        </table>

        <div *ngIf="!entity.fields?.length" style="margin-top: 8px; color:#444;">No fields.</div>

        <h3 style="margin-top: 22px;">Records</h3>

        <div style="display:flex; gap: 12px; flex-wrap: wrap; align-items: end;">
          <label style="flex: 1 1 520px;">
            New record JSON
            <textarea [value]="recordDraftJson" (input)="recordDraftJson = $any($event.target).value" rows="6" style="width: 100%; font-family: monospace;"></textarea>
          </label>
          <button type="button" (click)="createRecord()" [disabled]="creatingRecord || !entity">Add record</button>
          <div *ngIf="creatingRecord">Adding...</div>
        </div>

        <div *ngIf="recordError" style="margin-top: 8px; color:#b00020;">{{ recordError }}</div>

        <table *ngIf="records.length" style="width:100%; border-collapse: collapse; margin-top: 12px;">
          <thead>
            <tr>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Updated</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Id</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Data</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;"></th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let r of records">
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ r.updatedAtUtc | date: 'medium' }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px; font-family: monospace;">{{ r.entityRecordId }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">
                <textarea [value]="recordEdits[r.entityRecordId] !== undefined ? recordEdits[r.entityRecordId] : r.dataJson"
                          (input)="recordEdits[r.entityRecordId] = $any($event.target).value"
                          rows="6"
                          style="width: 100%; font-family: monospace;"></textarea>
              </td>
              <td style="border-bottom:1px solid #eee; padding: 6px; white-space: nowrap;">
                <button type="button" (click)="saveRecord(r.entityRecordId)" [disabled]="recordSaving[r.entityRecordId]">Save</button>
                <button type="button" (click)="deleteRecord(r.entityRecordId)" [disabled]="recordDeleting[r.entityRecordId]">Delete</button>
              </td>
            </tr>
          </tbody>
        </table>

        <div *ngIf="!records.length" style="margin-top: 8px; color:#444;">No records.</div>
      </section>
    </main>
  `,
})
export class LowCodeEntityDetailsPageComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  entity: EntityDefinitionDetailsDto | null = null;
  loading = false;
  savingEntity = false;
  deletingEntity = false;
  error: string | null = null;

  fieldError: string | null = null;
  creatingField = false;

  records: EntityRecordListItemDto[] = [];
  recordDraftJson = '{"name":"Acme Ltd","taxNumber":"123","riskScore":10,"status":"active"}';
  recordError: string | null = null;
  creatingRecord = false;
  recordEdits: Record<string, string> = {};
  recordSaving: Record<string, boolean> = {};
  recordDeleting: Record<string, boolean> = {};

  entityForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  fieldDraft = {
    name: '',
    fieldType: 'string',
    isRequired: false,
    maxLengthText: '',
  };

  fieldEdits: Record<string, Partial<FieldEdit>> = {};
  fieldSaving: Record<string, boolean> = {};
  fieldDeleting: Record<string, boolean> = {};

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  private get entityId(): string {
    const raw = this.route.snapshot.paramMap.get('id');
    return String(raw === null || raw === undefined ? '' : raw).trim();
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = null;
    this.fieldError = null;
    this.recordError = null;

    try {
      const id = this.entityId;
      if (!id) {
        this.error = 'Missing entity id.';
        this.entity = null;
        return;
      }

      this.entity = await firstValueFrom(this.http.get<EntityDefinitionDetailsDto>(`/api/entities/${id}`));
      this.entityForm.controls.name.setValue(this.entity.name === null || this.entity.name === undefined ? '' : this.entity.name);

      this.fieldEdits = {};
      this.fieldSaving = {};
      this.fieldDeleting = {};

      await this.loadRecords();
    } catch (e: any) {
      this.error = (e && e.error && e.error.message) ? e.error.message : (e && e.message ? e.message : 'Failed to load entity.');
      this.entity = null;
      this.records = [];
    } finally {
      this.loading = false;
    }
  }

  private isValidJsonObject(value: string): boolean {
    try {
      const parsed = JSON.parse(value);
      return parsed && typeof parsed === 'object' && !Array.isArray(parsed);
    } catch {
      return false;
    }
  }

  async loadRecords(): Promise<void> {
    if (!this.entity) {
      this.records = [];
      return;
    }

    try {
      const res = await firstValueFrom(
        this.http.get<EntityRecordListResponse>(`/api/entities/${this.entity.entityDefinitionId}/records`)
      );
      this.records = res.items === null || res.items === undefined ? [] : res.items;

      // keep edits for existing records only
      const nextEdits: Record<string, string> = {};
      for (const r of this.records) {
        if (this.recordEdits[r.entityRecordId] != null) nextEdits[r.entityRecordId] = this.recordEdits[r.entityRecordId];
      }
      this.recordEdits = nextEdits;
    } catch (e: any) {
      this.recordError = (e && e.error && e.error.message) ? e.error.message : (e && e.message ? e.message : 'Failed to load records.');
      this.records = [];
    }
  }

  async createRecord(): Promise<void> {
    if (!this.entity) return;

    this.creatingRecord = true;
    this.recordError = null;

    try {
      const dataJson = String(this.recordDraftJson === null || this.recordDraftJson === undefined ? '' : this.recordDraftJson).trim();
      if (!this.isValidJsonObject(dataJson)) {
        this.recordError = 'New record JSON must be a JSON object.';
        return;
      }

      await firstValueFrom(
        this.http.post(`/api/entities/${this.entity.entityDefinitionId}/records`, {
          dataJson,
        })
      );
      await this.loadRecords();
    } catch (e: any) {
      this.recordError = (e && e.error && e.error.message) ? e.error.message : (e && e.message ? e.message : 'Failed to create record.');
    } finally {
      this.creatingRecord = false;
    }
  }

  async saveRecord(recordId: string): Promise<void> {
    if (!this.entity) return;

    this.recordSaving[recordId] = true;
    this.recordError = null;

    try {
      const current = this.records.find(x => x.entityRecordId === recordId);
      if (!current) throw new Error('Record not found in UI state.');

      const candidate = this.recordEdits[recordId] !== undefined ? this.recordEdits[recordId] : current.dataJson;
      const dataJson = String(candidate === null || candidate === undefined ? '' : candidate).trim();
      if (!this.isValidJsonObject(dataJson)) {
        this.recordError = 'Record JSON must be a JSON object.';
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
      this.recordError = (e && e.error && e.error.message) ? e.error.message : (e && e.message ? e.message : 'Failed to save record.');
    } finally {
      this.recordSaving[recordId] = false;
    }
  }

  async deleteRecord(recordId: string): Promise<void> {
    if (!this.entity) return;

    this.recordDeleting[recordId] = true;
    this.recordError = null;

    try {
      await firstValueFrom(this.http.delete(`/api/entities/${this.entity.entityDefinitionId}/records/${recordId}`));
      delete this.recordEdits[recordId];
      await this.loadRecords();
    } catch (e: any) {
      this.recordError = (e && e.error && e.error.message) ? e.error.message : (e && e.message ? e.message : 'Failed to delete record.');
    } finally {
      this.recordDeleting[recordId] = false;
    }
  }

  async saveEntity(): Promise<void> {
    if (!this.entity) return;

    this.savingEntity = true;
    this.error = null;

    try {
      const id = this.entityId;
      const req = { name: this.entityForm.controls.name.value.trim() };
      this.entity = await firstValueFrom(this.http.put<EntityDefinitionDetailsDto>(`/api/entities/${id}`, req));
      this.entityForm.controls.name.setValue(this.entity.name === null || this.entity.name === undefined ? '' : this.entity.name);
    } catch (e: any) {
      this.error = (e && e.error && e.error.message) ? e.error.message : (e && e.message ? e.message : 'Failed to save entity.');
    } finally {
      this.savingEntity = false;
    }
  }

  async deleteEntity(): Promise<void> {
    if (!this.entity) return;

    this.deletingEntity = true;
    this.error = null;

    try {
      const id = this.entityId;
      await firstValueFrom(this.http.delete(`/api/entities/${id}`));
      await this.router.navigate(['/lowcode/entities']);
    } catch (e: any) {
      this.error = (e && e.error && e.error.message) ? e.error.message : (e && e.message ? e.message : 'Failed to delete entity.');
    } finally {
      this.deletingEntity = false;
    }
  }

  setFieldEdit(fieldId: string, key: keyof FieldEdit, value: any): void {
    if (!this.fieldEdits[fieldId]) this.fieldEdits[fieldId] = {};

    if (key === 'isRequired') {
      this.fieldEdits[fieldId][key] = !!value;
      return;
    }

    this.fieldEdits[fieldId][key] = String(value === null || value === undefined ? '' : value);
  }

  private parseMaxLength(value: string): number | null {
    const trimmed = String(value === null || value === undefined ? '' : value).trim();
    if (!trimmed) return null;
    const parsed = Number.parseInt(trimmed, 10);
    return Number.isFinite(parsed) ? parsed : null;
  }

  async createField(): Promise<void> {
    if (!this.entity) return;

    this.creatingField = true;
    this.fieldError = null;

    try {
      const req = {
        name: String(this.fieldDraft.name === null || this.fieldDraft.name === undefined ? '' : this.fieldDraft.name).trim(),
        fieldType: String(this.fieldDraft.fieldType === null || this.fieldDraft.fieldType === undefined ? '' : this.fieldDraft.fieldType).trim(),
        isRequired: !!this.fieldDraft.isRequired,
        maxLength: this.parseMaxLength(this.fieldDraft.maxLengthText),
      };

      await firstValueFrom(this.http.post(`/api/entities/${this.entity.entityDefinitionId}/fields`, req));

      this.fieldDraft.name = '';
      this.fieldDraft.fieldType = 'string';
      this.fieldDraft.isRequired = false;
      this.fieldDraft.maxLengthText = '';

      await this.load();
    } catch (e: any) {
      this.fieldError = (e && e.error && e.error.message) ? e.error.message : (e && e.message ? e.message : 'Failed to create field.');
    } finally {
      this.creatingField = false;
    }
  }

  async saveField(fieldId: string): Promise<void> {
    if (!this.entity) return;

    this.fieldSaving[fieldId] = true;
    this.fieldError = null;

    try {
      const f = this.entity.fields.find(x => x.fieldDefinitionId === fieldId);
      if (!f) throw new Error('Field not found in UI state.');

      const edit = this.fieldEdits[fieldId] === null || this.fieldEdits[fieldId] === undefined ? {} : this.fieldEdits[fieldId];

      const req = {
        name: (edit.name !== undefined ? edit.name : f.name).toString().trim(),
        fieldType: (edit.fieldType !== undefined ? edit.fieldType : f.fieldType).toString().trim(),
        isRequired: typeof edit.isRequired === 'boolean' ? edit.isRequired : !!f.isRequired,
        maxLength: this.parseMaxLength((edit.maxLength !== undefined ? edit.maxLength : (f.maxLength !== null && f.maxLength !== undefined ? f.maxLength : '')).toString()),
      };

      await firstValueFrom(this.http.put(`/api/entities/${this.entity.entityDefinitionId}/fields/${fieldId}`, req));
      await this.load();
    } catch (e: any) {
      this.fieldError = (e && e.error && e.error.message) ? e.error.message : (e && e.message ? e.message : 'Failed to save field.');
    } finally {
      this.fieldSaving[fieldId] = false;
    }
  }

  async deleteField(fieldId: string): Promise<void> {
    if (!this.entity) return;

    this.fieldDeleting[fieldId] = true;
    this.fieldError = null;

    try {
      await firstValueFrom(this.http.delete(`/api/entities/${this.entity.entityDefinitionId}/fields/${fieldId}`));
      await this.load();
    } catch (e: any) {
      this.fieldError = (e && e.error && e.error.message) ? e.error.message : (e && e.message ? e.message : 'Failed to delete field.');
    } finally {
      this.fieldDeleting[fieldId] = false;
    }
  }
}
