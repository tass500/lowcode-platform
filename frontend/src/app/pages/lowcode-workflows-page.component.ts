import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

type WorkflowDefinitionListItemDto = {
  workflowDefinitionId: string;
  name: string;
  createdAtUtc: string;
  updatedAtUtc: string;
};

type WorkflowListResponse = {
  serverTimeUtc: string;
  items: WorkflowDefinitionListItemDto[];
};

@Component({
  selector: 'app-lowcode-workflows-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <main style="padding: 16px; max-width: 980px; margin: 0 auto;">
      <div style="display:flex; gap: 12px; align-items: center; flex-wrap: wrap;">
        <h2 style="margin:0;">Workflows</h2>
        <a routerLink="/lowcode/workflows/new">New</a>
        <a routerLink="/lowcode/workflow-runs">All runs</a>
        <button type="button" (click)="load()" [disabled]="loading">Refresh</button>
        <div *ngIf="loading">Loading...</div>
        <div *ngIf="error" style="color:#b00020;">{{ error }}</div>
      </div>

      <div *ngIf="items.length > 0" style="margin-top: 14px; display: flex; flex-wrap: wrap; gap: 10px; align-items: center;">
        <label style="display: flex; align-items: center; gap: 8px; font-size: 14px; color: #333;">
          <span>Filter by name</span>
          <input
            type="search"
            [(ngModel)]="nameFilter"
            placeholder="Type to narrow the list…"
            autocomplete="off"
            style="min-width: 220px; padding: 6px 10px; box-sizing: border-box;"
          />
        </label>
        <span *ngIf="nameFilter.trim()" style="font-size: 13px; color: #666;">
          Showing {{ filteredItems.length }} of {{ items.length }}
        </span>
      </div>

      <section style="margin-top: 16px; padding: 12px; border: 1px solid #ddd; border-radius: 8px;">
        <div style="font-weight: 600; margin-bottom: 8px;">Import from export JSON</div>
        <p style="margin: 0 0 8px 0; font-size: 13px; color:#555;">
          Paste a package from <b>Export JSON</b> on workflow details, or load a <code>.json</code> file. Creates a <b>new</b> workflow (new id); you can change the name inside the JSON before importing.
        </p>
        <label style="display:block; margin-bottom: 8px; font-size: 13px;">
          <span style="color:#444;">File</span>
          <input type="file" accept=".json,application/json" (change)="onImportFile($event)" style="display:block; margin-top: 4px;" />
        </label>
        <textarea
          [(ngModel)]="importJsonText"
          rows="6"
          placeholder='{"exportFormatVersion":1,"name":"my-wf","definitionJson":"{...}"}'
          style="width: 100%; font-family: monospace; box-sizing: border-box;"
        ></textarea>
        <div style="margin-top: 8px; display:flex; gap: 10px; align-items: center; flex-wrap: wrap;">
          <button type="button" (click)="submitImport()" [disabled]="importing">Import</button>
          <div *ngIf="importing">Importing…</div>
          <div *ngIf="importError" style="color:#b00020;">{{ importError }}</div>
        </div>
      </section>

      <div *ngIf="items.length === 0 && !loading" style="margin-top: 12px; color:#444;">
        No workflows yet. Create one with <b>New</b> or <b>Import</b> above.
      </div>

      <div
        *ngIf="items.length > 0 && filteredItems.length === 0 && !loading"
        style="margin-top: 12px; color:#444;"
      >
        No workflows match this filter. Clear the search or try another name.
      </div>

      <table *ngIf="filteredItems.length > 0" style="width:100%; border-collapse: collapse; margin-top: 12px;">
        <thead>
          <tr>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Name</th>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Updated</th>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Id</th>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;"></th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let w of filteredItems">
            <td style="border-bottom:1px solid #eee; padding: 6px;">{{ w.name }}</td>
            <td style="border-bottom:1px solid #eee; padding: 6px;">{{ w.updatedAtUtc | date: 'medium' }}</td>
            <td style="border-bottom:1px solid #eee; padding: 6px; font-family: monospace;">{{ w.workflowDefinitionId }}</td>
            <td style="border-bottom:1px solid #eee; padding: 6px;">
              <a [routerLink]="['/lowcode/workflows', w.workflowDefinitionId]">Open</a>
            </td>
          </tr>
        </tbody>
      </table>
    </main>
  `,
})
export class LowCodeWorkflowsPageComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  items: WorkflowDefinitionListItemDto[] = [];
  /** Client-side filter (case-insensitive substring on workflow name). */
  nameFilter = '';
  loading = false;
  error: string | null = null;

  get filteredItems(): WorkflowDefinitionListItemDto[] {
    const q = this.nameFilter.trim().toLowerCase();
    if (!q) {
      return this.items;
    }
    return this.items.filter((w) => w.name.toLowerCase().includes(q));
  }

  importJsonText = '';
  importing = false;
  importError: string | null = null;

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const res = await firstValueFrom(this.http.get<WorkflowListResponse>('/api/workflows'));
      this.items = res.items ?? [];
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to load workflows.';
    } finally {
      this.loading = false;
    }
  }

  onImportFile(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => {
      this.importJsonText = String(reader.result ?? '');
      this.importError = null;
    };
    reader.readAsText(file);
    input.value = '';
  }

  async submitImport(): Promise<void> {
    this.importError = null;
    const raw = String(this.importJsonText ?? '').trim();
    if (!raw) {
      this.importError = 'Paste JSON or choose a file.';
      return;
    }

    let parsed: unknown;
    try {
      parsed = JSON.parse(raw);
    } catch {
      this.importError = 'Invalid JSON.';
      return;
    }

    const o = parsed as Record<string, unknown>;
    const name = String(o['name'] ?? '').trim();
    const definitionJson = o['definitionJson'];
    const exportFormatVersion =
      typeof o['exportFormatVersion'] === 'number' ? (o['exportFormatVersion'] as number) : null;

    if (!name || typeof definitionJson !== 'string') {
      this.importError = 'Package must include name (string) and definitionJson (string).';
      return;
    }

    this.importing = true;
    try {
      const res = await firstValueFrom(
        this.http.post<{ workflowDefinitionId: string }>('/api/workflows/import', {
          name,
          definitionJson,
          exportFormatVersion,
        }),
      );
      await this.router.navigate(['/lowcode/workflows', res.workflowDefinitionId]);
    } catch (e: any) {
      this.importError = e?.error?.message ?? e?.message ?? 'Import failed.';
    } finally {
      this.importing = false;
    }
  }
}
