import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

type TenantWorkflowRunListItemDto = {
  workflowRunId: string;
  workflowDefinitionId: string;
  workflowName: string;
  state: string;
  startedAtUtc: string | null;
  finishedAtUtc: string | null;
  traceId: string;
  errorCode: string | null;
  errorMessage: string | null;
};

type TenantWorkflowRunListResponse = {
  serverTimeUtc: string;
  items: TenantWorkflowRunListItemDto[];
  totalCount: number;
};

@Component({
  selector: 'app-lowcode-workflow-runs-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <main style="padding: 16px; max-width: 1100px; margin: 0 auto;">
      <div style="display:flex; gap: 12px; align-items: center; flex-wrap: wrap;">
        <h2 style="margin:0;">Workflow runs (tenant)</h2>
        <a routerLink="/lowcode/workflows">Workflows</a>
        <a routerLink="/lowcode/entities">Entities</a>
        <button type="button" (click)="load()" [disabled]="loading">Refresh</button>
        <div *ngIf="loading">Loading...</div>
        <div *ngIf="error" style="color:#b00020;">{{ error }}</div>
      </div>

      <section
        style="margin-top: 14px; padding: 12px; border: 1px solid #ddd; border-radius: 8px; background: #fafafa;"
      >
        <div style="font-weight: 600; margin-bottom: 8px;">Filters (server-side)</div>
        <div style="display: flex; flex-wrap: wrap; gap: 12px; align-items: flex-end;">
          <label style="display: flex; flex-direction: column; gap: 4px; font-size: 13px;">
            State
            <select [(ngModel)]="stateFilter" style="min-width: 160px; padding: 6px 8px;">
              <option value="">All</option>
              <option value="pending">pending</option>
              <option value="running">running</option>
              <option value="succeeded">succeeded</option>
              <option value="failed">failed</option>
              <option value="canceled">canceled</option>
            </select>
          </label>
          <label style="display: flex; flex-direction: column; gap: 4px; font-size: 13px;">
            Started after (local)
            <input type="datetime-local" [(ngModel)]="startedAfterLocal" style="padding: 6px 8px;" />
          </label>
          <label style="display: flex; flex-direction: column; gap: 4px; font-size: 13px;">
            Started before (local)
            <input type="datetime-local" [(ngModel)]="startedBeforeLocal" style="padding: 6px 8px;" />
          </label>
          <button type="button" (click)="applyFilters()">Apply</button>
          <button type="button" (click)="resetFilters()">Reset</button>
        </div>
        <p style="margin: 10px 0 0 0; font-size: 12px; color: #666;">
          Times are converted to UTC for the API. Pagination: {{ pageSize }} runs per page.
        </p>
      </section>

      <div *ngIf="totalCount >= 0 && !loading" style="margin-top: 12px; color:#444;">
        Total matching: {{ totalCount }}
        <span *ngIf="items.length > 0">
          — showing {{ rangeLabel }}
        </span>
      </div>

      <div
        *ngIf="totalCount === 0 && !loading && !error"
        style="margin-top: 12px; color:#444;"
      >
        <ng-container *ngIf="hasActiveFilters; else noRunsEver">
          No runs match these filters. Try <button type="button" (click)="resetFilters()" style="padding: 0; border: none; background: none; color: #0645ad; cursor: pointer; text-decoration: underline;">Reset</button> or widen the date range.
        </ng-container>
        <ng-template #noRunsEver>No workflow runs yet. Start a run from a workflow’s details page.</ng-template>
      </div>

      <div *ngIf="items.length > 0 && totalCount > 0" style="margin-top: 8px; display: flex; gap: 10px; align-items: center; flex-wrap: wrap;">
        <button type="button" (click)="prevPage()" [disabled]="loading || skip <= 0">Previous page</button>
        <button type="button" (click)="nextPage()" [disabled]="loading || !canNextPage">Next page</button>
      </div>

      <table *ngIf="items.length > 0" style="width:100%; border-collapse: collapse; margin-top: 12px; font-size: 14px;">
        <thead>
          <tr>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Workflow</th>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">State</th>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Started</th>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Run id</th>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;"></th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let r of items">
            <td style="border-bottom:1px solid #eee; padding: 6px;">{{ r.workflowName }}</td>
            <td style="border-bottom:1px solid #eee; padding: 6px;">{{ r.state }}</td>
            <td style="border-bottom:1px solid #eee; padding: 6px;">{{ r.startedAtUtc | date: 'medium' }}</td>
            <td style="border-bottom:1px solid #eee; padding: 6px; font-family: monospace; font-size: 12px;">{{ r.workflowRunId }}</td>
            <td style="border-bottom:1px solid #eee; padding: 6px;">
              <a [routerLink]="['/lowcode/runs', r.workflowRunId]">Open</a>
            </td>
          </tr>
        </tbody>
      </table>
    </main>
  `,
})
export class LowCodeWorkflowRunsPageComponent implements OnInit {
  private readonly http = inject(HttpClient);

  items: TenantWorkflowRunListItemDto[] = [];
  totalCount = -1;
  loading = false;
  error: string | null = null;

  readonly pageSize = 50;
  skip = 0;

  stateFilter = '';
  startedAfterLocal = '';
  startedBeforeLocal = '';

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  get hasActiveFilters(): boolean {
    return (
      !!this.stateFilter.trim() ||
      !!this.startedAfterLocal.trim() ||
      !!this.startedBeforeLocal.trim()
    );
  }

  get rangeLabel(): string {
    if (this.totalCount <= 0 || this.items.length === 0) {
      return '';
    }
    const from = this.skip + 1;
    const to = this.skip + this.items.length;
    return `${from}–${to}`;
  }

  get canNextPage(): boolean {
    return this.skip + this.pageSize < this.totalCount;
  }

  applyFilters(): void {
    this.skip = 0;
    void this.load();
  }

  resetFilters(): void {
    this.stateFilter = '';
    this.startedAfterLocal = '';
    this.startedBeforeLocal = '';
    this.skip = 0;
    void this.load();
  }

  prevPage(): void {
    if (this.skip <= 0) {
      return;
    }
    this.skip = Math.max(0, this.skip - this.pageSize);
    void this.load();
  }

  nextPage(): void {
    if (!this.canNextPage) {
      return;
    }
    this.skip += this.pageSize;
    void this.load();
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const params = new URLSearchParams();
      params.set('take', String(this.pageSize));
      params.set('skip', String(this.skip));

      const st = this.stateFilter.trim();
      if (st) {
        params.set('state', st);
      }

      const afterIso = this.toUtcIsoOrNull(this.startedAfterLocal);
      if (afterIso) {
        params.set('startedAfterUtc', afterIso);
      }

      const beforeIso = this.toUtcIsoOrNull(this.startedBeforeLocal);
      if (beforeIso) {
        params.set('startedBeforeUtc', beforeIso);
      }

      const res = await firstValueFrom(
        this.http.get<TenantWorkflowRunListResponse>(`/api/workflows/runs?${params.toString()}`),
      );
      this.items = res.items ?? [];
      this.totalCount = res.totalCount ?? 0;
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to load workflow runs.';
      this.items = [];
      this.totalCount = 0;
    } finally {
      this.loading = false;
    }
  }

  private toUtcIsoOrNull(localDatetimeValue: string): string | null {
    const t = String(localDatetimeValue ?? '').trim();
    if (!t) {
      return null;
    }
    const d = new Date(t);
    if (Number.isNaN(d.getTime())) {
      return null;
    }
    return d.toISOString();
  }
}
