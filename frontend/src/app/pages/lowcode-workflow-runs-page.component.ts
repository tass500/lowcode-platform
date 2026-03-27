import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
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
  imports: [CommonModule, RouterLink],
  template: `
    <main style="padding: 16px; max-width: 1100px; margin: 0 auto;">
      <div style="display:flex; gap: 12px; align-items: center; flex-wrap: wrap;">
        <h2 style="margin:0;">Workflow runs (tenant)</h2>
        <a routerLink="/lowcode/workflows">Workflows</a>
        <button type="button" (click)="load()" [disabled]="loading">Refresh</button>
        <div *ngIf="loading">Loading...</div>
        <div *ngIf="error" style="color:#b00020;">{{ error }}</div>
      </div>

      <div *ngIf="totalCount >= 0 && !loading" style="margin-top: 8px; color:#444;">
        Total: {{ totalCount }} (showing {{ items.length }} on this page)
      </div>

      <div *ngIf="items.length === 0 && !loading && !error" style="margin-top: 12px; color:#444;">
        No runs yet.
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

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const res = await firstValueFrom(
        this.http.get<TenantWorkflowRunListResponse>('/api/workflows/runs?take=50&skip=0'),
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
}
