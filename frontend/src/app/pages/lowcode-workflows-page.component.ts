import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
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
  imports: [CommonModule, RouterLink],
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

      <div *ngIf="items.length === 0 && !loading" style="margin-top: 12px; color:#444;">
        No workflows.
      </div>

      <table *ngIf="items.length > 0" style="width:100%; border-collapse: collapse; margin-top: 12px;">
        <thead>
          <tr>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Name</th>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Updated</th>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Id</th>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;"></th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let w of items">
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

  items: WorkflowDefinitionListItemDto[] = [];
  loading = false;
  error: string | null = null;

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
}
