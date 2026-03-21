import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

type WorkflowStepRunDto = {
  workflowStepRunId: string;
  stepKey: string;
  stepType: string;
  stepConfigJson?: string | null;
  outputJson?: string | null;
  state: string;
  attempt: number;
  lastErrorCode?: string | null;
  lastErrorMessage?: string | null;
  startedAtUtc?: string | null;
  finishedAtUtc?: string | null;
};

type WorkflowRunDetailsDto = {
  workflowRunId: string;
  workflowDefinitionId: string;
  state: string;
  startedAtUtc?: string | null;
  finishedAtUtc?: string | null;
  traceId: string;
  errorCode?: string | null;
  errorMessage?: string | null;
  steps: WorkflowStepRunDto[];
};

@Component({
  selector: 'app-lowcode-run-details-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <main style="padding: 16px; max-width: 980px; margin: 0 auto;">
      <div style="display:flex; gap: 12px; align-items: center; flex-wrap: wrap;">
        <a routerLink="/lowcode/workflows">← Workflows</a>
        <h2 style="margin:0;">Workflow run</h2>
        <button type="button" (click)="load()" [disabled]="loading">Refresh</button>
        <div *ngIf="polling" style="color:#444;">Polling...</div>
        <div *ngIf="loading">Loading...</div>
        <div *ngIf="error" style="color:#b00020;">{{ error }}</div>
      </div>

      <section *ngIf="run" style="margin-top: 12px;">
        <div style="display:flex; gap: 24px; flex-wrap: wrap; color:#444;">
          <div style="font-family: monospace;"><b>Run</b>: {{ run.workflowRunId }}</div>
          <div style="font-family: monospace;"><b>Workflow</b>: {{ run.workflowDefinitionId }}</div>
          <div><b>State</b>: {{ run.state }}</div>
          <div><b>Started</b>: {{ run.startedAtUtc | date: 'medium' }}</div>
          <div><b>Finished</b>: {{ run.finishedAtUtc | date: 'medium' }}</div>
          <div style="font-family: monospace;"><b>Trace</b>: {{ run.traceId }}</div>
        </div>

        <div *ngIf="run.errorCode || run.errorMessage" style="margin-top: 12px; background:#fff5f5; border: 1px solid #ffd6d6; padding: 12px; border-radius: 8px;">
          <div><b>Error</b>: {{ run.errorCode }}</div>
          <div style="white-space: pre-wrap;">{{ run.errorMessage }}</div>
        </div>

        <h3 style="margin-top: 16px;">Steps</h3>

        <div style="display:flex; gap: 12px; flex-wrap: wrap; align-items: end; margin-top: 8px;">
          <label>
            State
            <select [value]="stateFilter" (change)="stateFilter = $any($event.target).value">
              <option value="">(all)</option>
              <option value="pending">pending</option>
              <option value="running">running</option>
              <option value="succeeded">succeeded</option>
              <option value="failed">failed</option>
              <option value="canceled">canceled</option>
            </select>
          </label>

          <label>
            Type
            <input [value]="typeFilter" (input)="typeFilter = $any($event.target).value" placeholder="e.g. domainCommand" style="min-width: 240px;" />
          </label>

          <label>
            Search
            <input [value]="textFilter" (input)="textFilter = $any($event.target).value" placeholder="in errors/config" style="min-width: 260px;" />
          </label>
        </div>

        <table *ngIf="filteredSteps.length" style="width:100%; border-collapse: collapse; margin-top: 8px;">
          <thead>
            <tr>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Key</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Type</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">State</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Attempt</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Error</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;"></th>
            </tr>
          </thead>
          <tbody>
            <ng-container *ngFor="let s of filteredSteps">
              <tr>
              <td style="border-bottom:1px solid #eee; padding: 6px; font-family: monospace;">{{ s.stepKey }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ s.stepType }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">
                <span [style.color]="stateColor(s.state)"><b>{{ s.state }}</b></span>
              </td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ s.attempt }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px; white-space: pre-wrap;">
                <span *ngIf="s.lastErrorCode || s.lastErrorMessage">{{ s.lastErrorCode }} {{ s.lastErrorMessage }}</span>
              </td>
              <td style="border-bottom:1px solid #eee; padding: 6px; white-space: nowrap;">
                <button type="button" (click)="toggleStepConfig(s.workflowStepRunId)">Config</button>
                <button type="button" (click)="toggleStepOutput(s.workflowStepRunId)" [disabled]="!s.outputJson" style="margin-left: 6px;">Output</button>
              </td>
              </tr>

              <tr *ngIf="expandedStepIds[s.workflowStepRunId]">
                <td colspan="6" style="border-bottom:1px solid #eee; padding: 6px;">
                  <div style="color:#444; margin-bottom: 6px;"><b>Step config</b></div>
                  <div *ngIf="extractContextVars(s.stepConfigJson).length" style="margin-bottom: 6px; color:#444;">
                    <b>Context vars</b>:
                    <span style="font-family: monospace;">{{ extractContextVars(s.stepConfigJson).join(', ') }}</span>
                  </div>
                  <textarea [value]="formatJsonMaybe(s.stepConfigJson)" rows="8" style="width: 100%; font-family: monospace;" readonly></textarea>
                </td>
              </tr>

              <tr *ngIf="expandedOutputIds[s.workflowStepRunId]">
                <td colspan="6" style="border-bottom:1px solid #eee; padding: 6px;">
                  <div style="color:#444; margin-bottom: 6px;"><b>Step output</b></div>
                  <textarea [value]="formatJsonMaybe(s.outputJson)" rows="6" style="width: 100%; font-family: monospace;" readonly></textarea>
                </td>
              </tr>
            </ng-container>
          </tbody>
        </table>

        <div *ngIf="!filteredSteps.length" style="margin-top: 8px; color:#444;">No steps.</div>
      </section>
    </main>
  `,
})
export class LowCodeRunDetailsPageComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);

  run: WorkflowRunDetailsDto | null = null;
  loading = false;
  error: string | null = null;

  polling = false;
  private pollTimer: any = null;

  stateFilter = '';
  typeFilter = '';
  textFilter = '';

  expandedStepIds: Record<string, boolean> = {};
  expandedOutputIds: Record<string, boolean> = {};

  get filteredSteps(): WorkflowStepRunDto[] {
    const steps = this.run?.steps ?? [];
    const state = String(this.stateFilter ?? '').trim().toLowerCase();
    const type = String(this.typeFilter ?? '').trim().toLowerCase();
    const q = String(this.textFilter ?? '').trim().toLowerCase();

    return steps.filter(s => {
      if (state && String(s.state ?? '').toLowerCase() !== state) return false;
      if (type && !String(s.stepType ?? '').toLowerCase().includes(type)) return false;
      if (!q) return true;

      const err = `${s.lastErrorCode ?? ''} ${s.lastErrorMessage ?? ''}`.toLowerCase();
      const cfg = String(s.stepConfigJson ?? '').toLowerCase();
      const out = String(s.outputJson ?? '').toLowerCase();
      return err.includes(q) || cfg.includes(q) || out.includes(q);
    });
  }

  extractContextVars(value: string | null | undefined): string[] {
    const s = String(value ?? '');
    if (!s) return [];
    const matches = [...s.matchAll(/\$\{([^}]+)\}/g)].map(m => String(m[1] ?? '').trim()).filter(x => !!x);
    return Array.from(new Set(matches)).sort((a, b) => a.localeCompare(b));
  }

  async ngOnInit(): Promise<void> {
    await this.load();
    this.ensurePolling();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  private get runId(): string {
    return String(this.route.snapshot.paramMap.get('runId') ?? '').trim();
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const id = this.runId;
      if (!id) {
        this.error = 'Missing run id.';
        this.run = null;
        return;
      }

      this.run = await firstValueFrom(this.http.get<WorkflowRunDetailsDto>(`/api/workflows/runs/${id}`));
      this.ensurePolling();
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to load run.';
      this.run = null;
    } finally {
      this.loading = false;
    }
  }

  toggleStepConfig(stepRunId: string): void {
    this.expandedStepIds[stepRunId] = !this.expandedStepIds[stepRunId];
  }

  toggleStepOutput(stepRunId: string): void {
    this.expandedOutputIds[stepRunId] = !this.expandedOutputIds[stepRunId];
  }

  formatJsonMaybe(value: string | null | undefined): string {
    const raw = String(value ?? '').trim();
    if (!raw) return '';
    try {
      const parsed = JSON.parse(raw);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return raw;
    }
  }

  stateColor(state: string | null | undefined): string {
    const s = String(state ?? '').toLowerCase();
    if (s === 'succeeded') return '#1b5e20';
    if (s === 'failed') return '#b00020';
    if (s === 'running') return '#0b5394';
    if (s === 'pending') return '#666';
    return '#444';
  }

  private ensurePolling(): void {
    const state = (this.run?.state ?? '').toLowerCase();
    const shouldPoll = state === 'pending' || state === 'running';

    if (!shouldPoll) {
      this.stopPolling();
      return;
    }

    if (this.pollTimer) {
      this.polling = true;
      return;
    }

    this.polling = true;
    this.pollTimer = setInterval(async () => {
      await this.load();
      const s = (this.run?.state ?? '').toLowerCase();
      if (!(s === 'pending' || s === 'running')) {
        this.stopPolling();
      }
    }, 1000);
  }

  private stopPolling(): void {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
    this.polling = false;
  }
}
