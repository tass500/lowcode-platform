import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

type WorkflowDefinitionDetailsDto = {
  workflowDefinitionId: string;
  name: string;
  definitionJson: string;
  lintWarnings: { code: string; message: string }[];
  createdAtUtc: string;
  updatedAtUtc: string;
};

type StartWorkflowRunResponse = {
  serverTimeUtc: string;
  workflowRunId: string;
};

type WorkflowRunListItemDto = {
  workflowRunId: string;
  workflowDefinitionId: string;
  state: string;
  startedAtUtc?: string | null;
  finishedAtUtc?: string | null;
  traceId: string;
  errorCode?: string | null;
  errorMessage?: string | null;
};

type WorkflowRunListResponse = {
  serverTimeUtc: string;
  items: WorkflowRunListItemDto[];
};

type ApiErrorDetail = {
  path?: string | null;
  code: string;
  message: string;
  severity?: string | null;
};

@Component({
  selector: 'app-lowcode-workflow-details-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <main style="padding: 16px; max-width: 980px; margin: 0 auto;">
      <div style="display:flex; gap: 12px; align-items: center; flex-wrap: wrap;">
        <a routerLink="/lowcode/workflows">← Workflows</a>
        <h2 style="margin:0;">Workflow</h2>
        <button type="button" (click)="load()" [disabled]="loading">Refresh</button>
        <button type="button" (click)="startRun()" [disabled]="loading || starting || !workflow">Start run</button>
        <button type="button" (click)="save()" [disabled]="!workflow || form.invalid || saving">Save</button>
        <button type="button" (click)="delete()" [disabled]="!workflow || deleting">Delete</button>
        <span style="color:#999;">|</span>
        <button type="button" (click)="tab = 'definition'" [disabled]="tab === 'definition'">Definition</button>
        <button type="button" (click)="tab = 'runs'; loadRuns(true)" [disabled]="tab === 'runs'">Runs</button>
        <ng-container *ngIf="tab === 'definition'">
          <span style="color:#999;">|</span>
          <button type="button" (click)="viewMode = 'viewer'" [disabled]="viewMode === 'viewer'">Viewer</button>
          <button type="button" (click)="viewMode = 'json'" [disabled]="viewMode === 'json'">JSON</button>
        </ng-container>
        <div *ngIf="loading">Loading...</div>
        <div *ngIf="starting">Starting...</div>
        <div *ngIf="saving">Saving...</div>
        <div *ngIf="deleting">Deleting...</div>
        <div *ngIf="error" style="color:#b00020;">{{ error }}</div>
      </div>
      <section *ngIf="errorDetails.length > 0" style="margin-top: 10px; padding: 10px 12px; border: 1px solid #f3d5d5; border-radius: 8px; background: #fff7f7;">
        <div style="font-weight: 600; margin-bottom: 6px;">Validation details</div>
        <div *ngFor="let d of errorDetails" style="font-family: monospace; font-size: 12px; color:#8a1f1f; margin-bottom: 4px;">
          {{ d.path || '$' }} | {{ d.code }} | {{ d.message }}
        </div>
      </section>

      <div *ngIf="workflow" style="margin-top: 12px;">
        <div style="display:flex; gap: 24px; flex-wrap: wrap; color:#444;">
          <div><b>Name</b>: {{ workflow.name }}</div>
          <div><b>Updated</b>: {{ workflow.updatedAtUtc | date: 'medium' }}</div>
          <div style="font-family: monospace;"><b>Id</b>: {{ workflow.workflowDefinitionId }}</div>
        </div>

        <section *ngIf="workflow.lintWarnings?.length" style="margin-top: 10px; padding: 10px 12px; border: 1px solid #f0e0a0; border-radius: 8px; background: #fffaf0;">
          <div style="font-weight: 600; margin-bottom: 6px;">Lint warnings</div>
          <div *ngFor="let w of workflow.lintWarnings" style="font-family: monospace; color:#6b4e00;">
            {{ w.code }}: {{ w.message }}
          </div>
        </section>

        <ng-container *ngIf="tab === 'definition'">
          <form [formGroup]="form" style="margin-top: 12px; display:flex; flex-direction: column; gap: 12px;">
            <label>
              Name
              <input formControlName="name" style="width: 100%; max-width: 520px;" />
            </label>

            <ng-container *ngIf="viewMode === 'json'">
              <label>
                Definition JSON
                <div *ngIf="extractContextVars(form.controls.definitionJson.value).length" style="margin: 6px 0;">
                  <div style="font-weight: 600; color:#444; margin-bottom: 4px;">Context vars preview</div>
                  <pre style="margin:0; padding: 8px; border: 1px solid #ddd; border-radius: 8px; background:#fafafa; overflow:auto; font-family: monospace;" [innerHTML]="highlightContextVars(form.controls.definitionJson.value)"></pre>
                </div>
                <textarea #definitionJsonEl formControlName="definitionJson" rows="14" style="width: 100%; font-family: monospace;"></textarea>
              </label>

              <section *ngIf="contextVarSuggestions.length" style="padding: 10px 12px; border: 1px solid #eee; border-radius: 8px; background: #fafafa;">
                <div style="font-weight: 600; margin-bottom: 6px;">Context var suggestions</div>
                <div style="display:flex; gap: 8px; flex-wrap: wrap; align-items: center;">
                  <button
                    type="button"
                    *ngFor="let s of contextVarSuggestions"
                    (click)="insertContextVarSuggestion(definitionJsonEl, s)"
                    style="font-family: monospace;"
                  >
                    {{ '${' + s + '}' }}
                  </button>
                </div>
                <div style="margin-top: 6px; color:#444;">Click a suggestion to insert at cursor.</div>
              </section>
            </ng-container>
          </form>

          <section *ngIf="viewMode === 'viewer'" style="margin-top: 12px;">
            <div *ngIf="viewerError" style="color:#b00020;">{{ viewerError }}</div>
            <div *ngIf="!viewerError && viewerSteps.length === 0" style="color:#444;">No steps.</div>

            <div *ngIf="!viewerError && viewerSteps.length > 0" style="display:flex; gap: 10px; flex-wrap: wrap; align-items: center;">
              <ng-container *ngFor="let s of viewerSteps; let last = last">
                <div style="padding: 10px 12px; border: 1px solid #ddd; border-radius: 10px; background: #fafafa; min-width: 120px;">
                  <div style="font-family: monospace; color:#666;">{{ s.index }}</div>
                  <div><b>{{ s.label }}</b></div>
                  <div *ngIf="viewerStepWarnings(s.index).length > 0" style="margin-top: 6px;">
                    <div style="display:inline-block; font-size: 12px; padding: 2px 6px; border-radius: 999px; background:#fff4cc; color:#6b4e00;">
                      {{ viewerStepWarnings(s.index).length }} warning
                      <ng-container *ngIf="viewerStepWarnings(s.index).length !== 1">s</ng-container>
                    </div>
                    <div *ngFor="let w of viewerStepWarnings(s.index)" style="margin-top: 4px; font-size: 12px; color:#6b4e00;">
                      {{ w.code }}: {{ w.message }}
                    </div>
                  </div>
                </div>
                <div *ngIf="!last" style="color:#aaa; font-size: 18px;">→</div>
              </ng-container>
            </div>
          </section>
        </ng-container>

        <ng-container *ngIf="tab === 'runs'">
          <div style="margin-top: 12px; display:flex; gap: 12px; align-items: center; flex-wrap: wrap;">
            <button type="button" (click)="loadRuns(true)" [disabled]="runsLoading">Refresh runs</button>
            <a *ngIf="lastRunId" [routerLink]="['/lowcode/runs', lastRunId]">Open latest</a>
            <div *ngIf="runsLoading">Loading runs...</div>
            <div *ngIf="runsError" style="color:#b00020;">{{ runsError }}</div>
          </div>

          <div *ngIf="!runsLoading && runs.length === 0" style="margin-top: 12px; color:#444;">No runs.</div>

          <table *ngIf="runs.length > 0" style="width:100%; border-collapse: collapse; margin-top: 12px;">
            <thead>
              <tr>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">State</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Started</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Finished</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">RunId</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">TraceId</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Error</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;"></th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let r of runs">
                <td style="border-bottom:1px solid #eee; padding: 6px;">
                  <span [style.color]="stateColor(r.state)"><b>{{ r.state }}</b></span>
                </td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">{{ r.startedAtUtc | date: 'medium' }}</td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">{{ r.finishedAtUtc | date: 'medium' }}</td>
                <td style="border-bottom:1px solid #eee; padding: 6px; font-family: monospace;">{{ r.workflowRunId }}</td>
                <td style="border-bottom:1px solid #eee; padding: 6px; font-family: monospace;">{{ r.traceId }}</td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">
                  <div *ngIf="r.errorCode || r.errorMessage">
                    <div style="font-family: monospace;">{{ r.errorCode }}</div>
                    <div>{{ r.errorMessage }}</div>
                  </div>
                </td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">
                  <a [routerLink]="['/lowcode/runs', r.workflowRunId]">Open</a>
                </td>
              </tr>
            </tbody>
          </table>
        </ng-container>
      </div>
    </main>
  `,
})
export class LowCodeWorkflowDetailsPageComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  workflow: WorkflowDefinitionDetailsDto | null = null;
  loading = false;
  starting = false;
  saving = false;
  deleting = false;
  error: string | null = null;
  errorDetails: ApiErrorDetail[] = [];

  viewMode: 'viewer' | 'json' = 'viewer';
  tab: 'definition' | 'runs' = 'definition';

  runs: WorkflowRunListItemDto[] = [];
  runsLoading = false;
  runsError: string | null = null;

  private runsPollTimer: any = null;

  lastRunId: string | null = null;

  form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    definitionJson: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  get contextVarSuggestions(): string[] {
    const raw = String(this.form.controls.definitionJson.value ?? '');
    try {
      const parsed: any = JSON.parse(raw);
      const steps: any[] = Array.isArray(parsed?.steps) ? parsed.steps : [];

      const suggestions: string[] = [];
      for (let i = 0; i < steps.length; i += 1) {
        const key = String(i).padStart(3, '0');
        suggestions.push(key);

        const step = steps[i];
        const type = String(step?.type ?? '').toLowerCase();

        if (type === 'set' && step?.output && typeof step.output === 'object' && !Array.isArray(step.output)) {
          for (const k of Object.keys(step.output)) {
            if (!k) continue;
            suggestions.push(`${key}.${k}`);
          }
        }

        if (type === 'map' && step?.mappings && typeof step.mappings === 'object' && !Array.isArray(step.mappings)) {
          for (const k of Object.keys(step.mappings)) {
            if (!k) continue;
            suggestions.push(`${key}.${k}`);
          }
        }
      }

      return Array.from(new Set(suggestions)).sort((a, b) => a.localeCompare(b));
    } catch {
      return [];
    }
  }

  insertContextVarSuggestion(el: HTMLTextAreaElement, suggestion: string): void {
    const token = `\${${suggestion}}`;
    const start = el.selectionStart ?? 0;
    const end = el.selectionEnd ?? start;
    const current = this.form.controls.definitionJson.value ?? '';
    const next = current.substring(0, start) + token + current.substring(end);
    this.form.controls.definitionJson.setValue(next);

    queueMicrotask(() => {
      el.focus();
      const pos = start + token.length;
      el.setSelectionRange(pos, pos);
    });
  }

  get viewerError(): string | null {
    try {
      const raw = this.form.controls.definitionJson.value ?? '';
      const parsed = JSON.parse(raw);
      const steps = parsed?.steps;
      if (!Array.isArray(steps)) return null;
      return null;
    } catch (e: any) {
      return e?.message ?? 'Invalid JSON.';
    }
  }

  get viewerSteps(): Array<{ index: number; type: string; label: string }> {
    try {
      const raw = this.form.controls.definitionJson.value ?? '';
      const parsed = JSON.parse(raw);
      const steps = parsed?.steps;
      if (!Array.isArray(steps)) return [];

      return steps.map((s: any, i: number) => {
        const type = typeof s?.type === 'string' ? s.type : 'noop';
        let label = type;
        if (type.toLowerCase() === 'delay' && typeof s?.ms === 'number') {
          label = `delay (${s.ms}ms)`;
        }
        return { index: i, type, label };
      });
    } catch {
      return [];
    }
  }

  viewerStepWarnings(stepIndex: number): Array<{ code: string; message: string }> {
    const key = String(stepIndex).padStart(3, '0');
    const warnings = this.workflow?.lintWarnings ?? [];
    return warnings.filter(w => {
      const message = String(w?.message ?? '');
      return message.includes(`'${key}'`) || message.includes(`\${${key}`);
    });
  }

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  ngOnDestroy(): void {
    this.stopRunsPolling();
  }

  private get workflowId(): string {
    return String(this.route.snapshot.paramMap.get('id') ?? '').trim();
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = null;
    this.errorDetails = [];

    try {
      const id = this.workflowId;
      if (!id) {
        this.error = 'Missing workflow id.';
        this.workflow = null;
        return;
      }

      this.workflow = await firstValueFrom(this.http.get<WorkflowDefinitionDetailsDto>(`/api/workflows/${id}`));
      this.form.controls.name.setValue(this.workflow.name ?? '');
      this.form.controls.definitionJson.setValue(this.workflow.definitionJson ?? '{}');

      if (this.tab === 'runs')
        await this.loadRuns(true);
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to load workflow.';
      this.errorDetails = Array.isArray(e?.error?.details) ? (e.error.details as ApiErrorDetail[]) : [];
      this.workflow = null;
    } finally {
      this.loading = false;
    }
  }

  async loadRuns(startPolling: boolean): Promise<void> {
    if (!this.workflow) {
      this.runs = [];
      return;
    }

    this.runsLoading = true;
    this.runsError = null;

    try {
      const id = this.workflowId;
      const res = await firstValueFrom(this.http.get<WorkflowRunListResponse>(`/api/workflows/${id}/runs`));
      this.runs = res.items ?? [];

      this.lastRunId = this.runs.length > 0 ? this.runs[0].workflowRunId : null;

      if (startPolling)
        this.startRunsPollingIfNeeded();
    } catch (e: any) {
      this.runsError = e?.error?.message ?? e?.message ?? 'Failed to load runs.';
      this.runs = [];
    } finally {
      this.runsLoading = false;
    }
  }

  private startRunsPollingIfNeeded(): void {
    if (this.runsPollTimer) return;

    const hasRunning = this.runs.some(r => (r.state ?? '').toLowerCase() === 'running');
    if (!hasRunning) return;

    this.runsPollTimer = setInterval(async () => {
      if (this.tab !== 'runs' || !this.workflow) {
        this.stopRunsPolling();
        return;
      }

      await this.loadRuns(false);

      const stillRunning = this.runs.some(r => (r.state ?? '').toLowerCase() === 'running');
      if (!stillRunning)
        this.stopRunsPolling();
    }, 2000);
  }

  private stopRunsPolling(): void {
    if (!this.runsPollTimer) return;
    clearInterval(this.runsPollTimer);
    this.runsPollTimer = null;
  }

  stateColor(state: string | null | undefined): string {
    const s = String(state ?? '').toLowerCase();
    if (s === 'succeeded') return '#1b5e20';
    if (s === 'failed') return '#b00020';
    if (s === 'running') return '#0b5394';
    if (s === 'pending') return '#666';
    return '#444';
  }

  private escapeHtml(s: string): string {
    return s
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  extractContextVars(value: string | null | undefined): string[] {
    const s = String(value ?? '');
    if (!s.includes('${')) return [];
    const matches = [...s.matchAll(/\$\{([^}]*)\}/g)].map(m => String(m[1] ?? '').trim()).filter(x => !!x);
    return Array.from(new Set(matches)).sort((a, b) => a.localeCompare(b));
  }

  highlightContextVars(value: string | null | undefined): string {
    const s = String(value ?? '');
    const escaped = this.escapeHtml(s);
    return escaped.replace(/\$\{[^}]*\}/g, m => `<span style="background:#fff4cc; border:1px solid #f0d98a; border-radius:4px; padding:0 2px;">${m}</span>`);
  }

  private validateContextVarSyntax(value: string): string | null {
    if (!value.includes('${')) return null;

    let idx = 0;
    while (true) {
      const start = value.indexOf('${', idx);
      if (start < 0) break;
      const end = value.indexOf('}', start + 2);
      if (end < 0) return "Invalid context variable syntax: missing closing '}' in '${...}'.";
      const inner = value.substring(start + 2, end);
      if (!inner.trim()) return "Invalid context variable syntax: empty path in '${...}'.";
      idx = end + 1;
    }

    for (const m of value.matchAll(/\$\{([^}]*)\}/g)) {
      const inner = String(m[1] ?? '');
      if (!inner.trim()) return "Invalid context variable syntax: empty path in '${...}'.";
    }

    return null;
  }

  async save(): Promise<void> {
    if (!this.workflow) return;

    const preflight = this.validateContextVarSyntax(this.form.controls.definitionJson.value ?? '');
    if (preflight) {
      this.error = preflight;
      this.errorDetails = [];
      return;
    }

    this.saving = true;
    this.error = null;
    this.errorDetails = [];

    try {
      const id = this.workflowId;
      const req = {
        name: this.form.controls.name.value.trim(),
        definitionJson: this.form.controls.definitionJson.value.trim(),
      };

      this.workflow = await firstValueFrom(this.http.put<WorkflowDefinitionDetailsDto>(`/api/workflows/${id}`, req));
      this.form.controls.name.setValue(this.workflow.name ?? '');
      this.form.controls.definitionJson.setValue(this.workflow.definitionJson ?? '{}');
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to save workflow.';
      this.errorDetails = Array.isArray(e?.error?.details) ? (e.error.details as ApiErrorDetail[]) : [];
    } finally {
      this.saving = false;
    }
  }

  async delete(): Promise<void> {
    if (!this.workflow) return;

    this.deleting = true;
    this.error = null;
    this.errorDetails = [];

    try {
      const id = this.workflowId;
      await firstValueFrom(this.http.delete(`/api/workflows/${id}`));
      await this.router.navigate(['/lowcode/workflows']);
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to delete workflow.';
      this.errorDetails = Array.isArray(e?.error?.details) ? (e.error.details as ApiErrorDetail[]) : [];
    } finally {
      this.deleting = false;
    }
  }

  async startRun(): Promise<void> {
    this.starting = true;
    this.error = null;
    this.errorDetails = [];

    try {
      const id = this.workflowId;
      const res = await firstValueFrom(
        this.http.post<StartWorkflowRunResponse>(`/api/workflows/${id}/runs`, null)
      );

      this.lastRunId = res.workflowRunId;

      this.tab = 'runs';
      await this.loadRuns(true);
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to start run.';
      this.errorDetails = Array.isArray(e?.error?.details) ? (e.error.details as ApiErrorDetail[]) : [];
    } finally {
      this.starting = false;
    }
  }
}
