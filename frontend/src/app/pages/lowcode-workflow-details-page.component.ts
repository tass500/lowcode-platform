import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ChangeDetectorRef, Component, ElementRef, inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { buildMergedContextVarSuggestions } from './lowcode-workflow-context-suggestions';
import { groupLintWarningsByCode, type LintWarningGroup } from './lowcode-workflow-lint-utils';
import { minifyWorkflowDefinitionJson, prettifyWorkflowDefinitionJson } from './lowcode-workflow-json-form';
import {
  appendBuilderStep,
  moveBuilderStep,
  parseBuilderStepSummaries,
  removeBuilderStepAt,
  WORKFLOW_BUILDER_PALETTE,
  type BuilderStepType,
} from './lowcode-workflow-builder-utils';
import {
  buildWorkflowViewerStepCards,
  findCaretIndexForWorkflowStep,
  type WorkflowViewerStepCard,
} from './lowcode-workflow-viewer-utils';

type WorkflowDefinitionDetailsDto = {
  workflowDefinitionId: string;
  name: string;
  definitionJson: string;
  lintWarnings: { code: string; message: string }[];
  inboundTriggerConfigured: boolean;
  scheduleEnabled: boolean;
  scheduleCron: string | null;
  scheduleNextDueUtc: string | null;
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
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  template: `
    <main style="padding: 16px; max-width: 980px; margin: 0 auto;">
      <div style="display:flex; gap: 12px; align-items: center; flex-wrap: wrap;">
        <a routerLink="/lowcode/workflows">← Workflows</a>
        <h2 style="margin:0;">Workflow</h2>
        <button type="button" (click)="load()" [disabled]="loading">Refresh</button>
        <button type="button" (click)="startRun()" [disabled]="loading || starting || !workflow">Start run</button>
        <button type="button" (click)="save()" [disabled]="!workflow || form.invalid || saving">Save</button>
        <button type="button" (click)="exportPack()" [disabled]="!workflow || exporting">Export JSON</button>
        <button type="button" (click)="delete()" [disabled]="!workflow || deleting">Delete</button>
        <div *ngIf="exportError" style="color:#b00020;">{{ exportError }}</div>
        <span style="color:#999;">|</span>
        <button type="button" (click)="tab = 'definition'" [disabled]="tab === 'definition'">Definition</button>
        <button type="button" (click)="tab = 'runs'; loadRuns(true)" [disabled]="tab === 'runs'">Runs</button>
        <ng-container *ngIf="tab === 'definition'">
          <span style="color:#999;">|</span>
          <button type="button" (click)="viewMode = 'viewer'" [disabled]="viewMode === 'viewer'">Viewer</button>
          <button type="button" (click)="viewMode = 'builder'" [disabled]="viewMode === 'builder'">Builder</button>
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
        <div style="margin-top: 8px; font-size: 13px; color:#555;">
          <b>Inbound trigger</b>:
          <span *ngIf="workflow.inboundTriggerConfigured" style="color:#0a6;">configured</span>
          <span *ngIf="!workflow.inboundTriggerConfigured">not set</span>
          <span style="color:#999;"> — </span>
          <code style="font-size: 12px;">POST /api/inbound/workflows/{{ workflow.workflowDefinitionId }}/runs</code>
          <span style="color:#999;">header</span>           <code style="font-size: 12px;">X-Workflow-Inbound-Secret</code>
        </div>

        <div *ngIf="workflow" style="margin-top: 12px; padding: 10px 12px; border: 1px solid #e0e8f0; border-radius: 8px; background: #f8fbff;">
          <div style="font-weight: 600; margin-bottom: 8px;">Schedule (UTC, MVP cron)</div>
          <div style="font-size: 12px; color:#666; margin-bottom: 8px;">
            Patterns: <code>* * * * *</code> (every minute), <code>*/5 * * * *</code>, <code>15 * * * *</code> (hourly :15),
            <code>30 9 * * *</code> (daily 09:30 UTC). Day/month/dow must be <code>*</code>.
          </div>
          <label style="display:flex; gap:8px; align-items:center; margin-bottom:8px;">
            <input type="checkbox" [(ngModel)]="scheduleEnabled" [ngModelOptions]="{ standalone: true }" />
            <span>Enabled</span>
          </label>
          <div style="margin-bottom:8px;">
            <label style="display:block; font-size: 12px; color:#555; margin-bottom:4px;">Cron (5 fields)</label>
            <input
              type="text"
              [(ngModel)]="scheduleCron"
              [ngModelOptions]="{ standalone: true }"
              [disabled]="!scheduleEnabled"
              style="width:100%; max-width: 420px; font-family: monospace; padding: 6px 8px;"
              placeholder="*/15 * * * *"
            />
          </div>
          <div *ngIf="workflow.scheduleNextDueUtc" style="font-size: 12px; color:#555; margin-bottom:8px;">
            Next due: {{ workflow.scheduleNextDueUtc | date: 'medium' : 'UTC' }}
          </div>
          <button type="button" (click)="saveSchedule()" [disabled]="scheduleSaving || !workflow">
            {{ scheduleSaving ? 'Saving…' : 'Save schedule' }}
          </button>
        </div>

        <section *ngIf="workflow.lintWarnings?.length" style="margin-top: 10px; padding: 10px 12px; border: 1px solid #f0e0a0; border-radius: 8px; background: #fffaf0;">
          <div style="display:flex; flex-wrap: wrap; gap: 8px 16px; align-items: baseline; margin-bottom: 6px;">
            <div style="font-weight: 600;">Lint warnings</div>
            <span style="font-size: 12px; padding: 2px 8px; border-radius: 999px; background:#fff4cc; color:#6b4e00;">
              {{ workflow.lintWarnings.length }} total
            </span>
          </div>
          <div style="font-size: 12px; color:#666; margin-bottom: 8px;">
            From last server response (Refresh / Save updates lint).
          </div>
          <div *ngFor="let g of lintWarningsGrouped" style="margin-top: 8px;">
            <div style="font-weight: 600; font-family: monospace; color:#6b4e00;">
              {{ g.code }} <span style="font-weight: 400; color:#888;">(×{{ g.count }})</span>
            </div>
            <div
              *ngFor="let m of g.messages"
              style="margin-top: 4px; padding-left: 8px; border-left: 2px solid #f0e0a0; font-family: monospace; font-size: 12px; color:#6b4e00; word-break: break-word; white-space: pre-wrap;"
            >
              {{ m }}
            </div>
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
                <div style="margin: 6px 0; display:flex; gap: 8px; flex-wrap: wrap; align-items: center;">
                  <button type="button" (click)="prettifyDefinitionJson()">Prettify</button>
                  <button type="button" (click)="minifyDefinitionJson()">Minify</button>
                  <span *ngIf="jsonFormatError" style="color:#b00020; font-size: 13px;">{{ jsonFormatError }}</span>
                </div>
                <div *ngIf="extractContextVars(form.controls.definitionJson.value).length" style="margin: 6px 0;">
                  <div style="font-weight: 600; color:#444; margin-bottom: 4px;">Context vars preview</div>
                  <pre style="margin:0; padding: 8px; border: 1px solid #ddd; border-radius: 8px; background:#fafafa; overflow:auto; font-family: monospace;" [innerHTML]="highlightContextVars(form.controls.definitionJson.value)"></pre>
                </div>
                <textarea #definitionJsonEl formControlName="definitionJson" rows="14" style="width: 100%; font-family: monospace;"></textarea>
              </label>

              <section *ngIf="contextVarSuggestions.length" style="padding: 10px 12px; border: 1px solid #eee; border-radius: 8px; background: #fafafa;">
                <div style="font-weight: 600; margin-bottom: 6px;">Context var suggestions</div>
                <label style="display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin-bottom: 8px;">
                  <span style="font-size: 13px; color: #444;">Autocomplete</span>
                  <input
                    type="text"
                    [attr.list]="contextVarDatalistId"
                    placeholder="Type to filter, pick a row…"
                    #ctxAutocomplete
                    (change)="onContextVarAutocompletePick(ctxAutocomplete, definitionJsonEl)"
                    style="min-width: 220px; font-family: monospace;"
                  />
                </label>
                <datalist [attr.id]="contextVarDatalistId">
                  <option *ngFor="let s of contextVarSuggestions" [value]="s"></option>
                </datalist>
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
                <div style="margin-top: 6px; color:#444;">Pick from the list or click a chip — inserts the <code>path</code> token at the cursor.</div>
              </section>
            </ng-container>
          </form>

          <section *ngIf="viewMode === 'builder'" style="margin-top: 12px;">
            <div *ngIf="viewerError" style="color:#b00020;">{{ viewerError }}</div>
            <div *ngIf="jsonFormatError && !viewerError" style="color:#b00020;">{{ jsonFormatError }}</div>
            <ng-container *ngIf="!viewerError">
              <p style="font-size: 13px; color:#555; margin: 0 0 12px 0; line-height: 1.45;">
                Steps run top to bottom. Runtime keys are <code>000</code>, <code>001</code>, … by order.
                Reordering or adding steps can break references to other steps (context paths) — switch to JSON to adjust.
              </p>
              <div style="margin-bottom: 12px;">
                <div style="font-weight: 600; margin-bottom: 6px;">Add step</div>
                <div style="display:flex; flex-wrap: wrap; gap: 6px;">
                  <button type="button" *ngFor="let p of builderPalette" (click)="addBuilderStep(p.type)">+ {{ p.label }}</button>
                </div>
              </div>
              <div *ngIf="builderStepRows.length === 0" style="color:#444;">No steps yet.</div>
              <div *ngIf="builderStepRows.length > 0" style="display:flex; flex-direction: column; gap: 8px;">
                <div
                  *ngFor="let row of builderStepRows"
                  style="display:flex; align-items:center; gap: 10px; flex-wrap: wrap; padding: 10px 12px; border: 1px solid #ddd; border-radius: 8px; background: #fafafa;"
                >
                  <span style="font-family: monospace; color:#666;">{{ formatBuilderStepKey(row.index) }}</span>
                  <span style="font-family: monospace;"><b>{{ row.type }}</b></span>
                  <span style="flex:1"></span>
                  <button type="button" (click)="moveBuilderStepUp(row.index)" [disabled]="row.index === 0">↑</button>
                  <button type="button" (click)="moveBuilderStepDown(row.index)" [disabled]="row.index === builderStepRows.length - 1">↓</button>
                  <button type="button" (click)="removeBuilderStep(row.index)" style="color:#b00020;">Remove</button>
                  <button type="button" (click)="jumpToJsonStep(row.index)">JSON →</button>
                </div>
              </div>
            </ng-container>
          </section>

          <section *ngIf="viewMode === 'viewer'" style="margin-top: 12px;">
            <div *ngIf="viewerError" style="color:#b00020;">{{ viewerError }}</div>
            <div *ngIf="!viewerError && viewerSteps.length === 0" style="color:#444;">No steps.</div>

            <div *ngIf="!viewerError && viewerSteps.length > 0" style="display:flex; gap: 10px; flex-wrap: wrap; align-items: stretch;">
              <ng-container *ngFor="let s of viewerSteps; let last = last">
                <div
                  style="padding: 10px 12px; border: 1px solid #ddd; border-radius: 10px; background: #fafafa; min-width: 140px; max-width: 280px;"
                >
                  <div style="display:flex; justify-content: space-between; align-items: flex-start; gap: 8px;">
                    <div style="font-family: monospace; color:#666;">{{ s.stepKey }}</div>
                    <button type="button" (click)="jumpToJsonStep(s.index)" style="font-size: 0.75rem; white-space: nowrap;">JSON →</button>
                  </div>
                  <div style="margin-top: 4px;"><b>{{ s.title }}</b></div>
                  <div *ngIf="s.subtitle" style="margin-top: 4px; font-size: 12px; color:#555; word-break: break-word; line-height: 1.35;">
                    {{ s.subtitle }}
                  </div>
                  <div
                    *ngIf="s.branchPreview"
                    style="margin-top: 6px; font-size: 12px; color:#0b5394; word-break: break-word; line-height: 1.35;"
                  >
                    {{ s.branchPreview }}
                  </div>
                  <div *ngIf="viewerStepWarnings(s.index).length > 0" style="margin-top: 6px;">
                    <div style="display:inline-block; font-size: 12px; padding: 2px 6px; border-radius: 999px; background:#fff4cc; color:#6b4e00;">
                      {{ viewerStepWarnings(s.index).length }} warning
                      <ng-container *ngIf="viewerStepWarnings(s.index).length !== 1">s</ng-container>
                    </div>
                    <div
                      *ngFor="let w of viewerStepWarnings(s.index)"
                      style="margin-top: 4px; font-size: 12px; color:#6b4e00; word-break: break-word; white-space: pre-wrap;"
                    >
                      <span style="font-weight: 600;">{{ w.code }}:</span> {{ w.message }}
                    </div>
                  </div>
                </div>
                <div *ngIf="!last" style="color:#aaa; font-size: 18px; align-self: center;">→</div>
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
  private readonly cdr = inject(ChangeDetectorRef);

  @ViewChild('definitionJsonEl') definitionJsonEl?: ElementRef<HTMLTextAreaElement>;

  readonly contextVarDatalistId = 'wf-ctx-suggestions-details';

  workflow: WorkflowDefinitionDetailsDto | null = null;
  loading = false;
  starting = false;
  saving = false;
  deleting = false;
  exporting = false;
  exportError: string | null = null;
  error: string | null = null;
  errorDetails: ApiErrorDetail[] = [];

  viewMode: 'viewer' | 'json' | 'builder' = 'viewer';

  readonly builderPalette = WORKFLOW_BUILDER_PALETTE;

  get builderStepRows(): Array<{ index: number; type: string }> {
    return parseBuilderStepSummaries(String(this.form.controls.definitionJson.value ?? ''));
  }

  formatBuilderStepKey(index: number): string {
    return String(index).padStart(3, '0');
  }

  addBuilderStep(type: BuilderStepType): void {
    this.jsonFormatError = null;
    try {
      const raw = String(this.form.controls.definitionJson.value ?? '');
      this.form.controls.definitionJson.setValue(appendBuilderStep(raw, type));
    } catch (e: any) {
      this.jsonFormatError = e?.message ?? 'Invalid JSON.';
    }
  }

  removeBuilderStep(index: number): void {
    this.jsonFormatError = null;
    try {
      const raw = String(this.form.controls.definitionJson.value ?? '');
      this.form.controls.definitionJson.setValue(removeBuilderStepAt(raw, index));
    } catch (e: any) {
      this.jsonFormatError = e?.message ?? 'Invalid JSON.';
    }
  }

  moveBuilderStepUp(index: number): void {
    if (index <= 0) return;
    this.applyBuilderMove(index, index - 1);
  }

  moveBuilderStepDown(index: number): void {
    this.applyBuilderMove(index, index + 1);
  }

  private applyBuilderMove(from: number, to: number): void {
    this.jsonFormatError = null;
    try {
      const raw = String(this.form.controls.definitionJson.value ?? '');
      this.form.controls.definitionJson.setValue(moveBuilderStep(raw, from, to));
    } catch (e: any) {
      this.jsonFormatError = e?.message ?? 'Invalid JSON.';
    }
  }
  tab: 'definition' | 'runs' = 'definition';

  runs: WorkflowRunListItemDto[] = [];
  runsLoading = false;
  runsError: string | null = null;

  private runsPollTimer: any = null;

  lastRunId: string | null = null;

  jsonFormatError: string | null = null;

  scheduleEnabled = false;
  scheduleCron = '';
  scheduleSaving = false;

  form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    definitionJson: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  get lintWarningsGrouped(): LintWarningGroup[] {
    return groupLintWarningsByCode(this.workflow?.lintWarnings);
  }

  get contextVarSuggestions(): string[] {
    return buildMergedContextVarSuggestions(String(this.form.controls.definitionJson.value ?? ''));
  }

  prettifyDefinitionJson(): void {
    this.jsonFormatError = null;
    try {
      const raw = String(this.form.controls.definitionJson.value ?? '');
      this.form.controls.definitionJson.setValue(prettifyWorkflowDefinitionJson(raw));
    } catch (e: any) {
      this.jsonFormatError = e?.message ?? 'Invalid JSON.';
    }
  }

  minifyDefinitionJson(): void {
    this.jsonFormatError = null;
    try {
      const raw = String(this.form.controls.definitionJson.value ?? '');
      this.form.controls.definitionJson.setValue(minifyWorkflowDefinitionJson(raw));
    } catch (e: any) {
      this.jsonFormatError = e?.message ?? 'Invalid JSON.';
    }
  }

  onContextVarAutocompletePick(pick: HTMLInputElement, el: HTMLTextAreaElement): void {
    const v = pick.value?.trim() ?? '';
    if (!v) return;
    if (!this.contextVarSuggestions.includes(v)) return;
    this.insertContextVarSuggestion(el, v);
    pick.value = '';
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
      if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed))
        return 'Definition must be a JSON object.';
      const steps = (parsed as { steps?: unknown }).steps;
      if (steps === undefined) return 'Definition must include a `steps` array.';
      if (!Array.isArray(steps)) return 'Definition `steps` must be an array.';
      return null;
    } catch (e: any) {
      return e?.message ?? 'Invalid JSON.';
    }
  }

  get viewerSteps(): WorkflowViewerStepCard[] {
    try {
      const raw = this.form.controls.definitionJson.value ?? '';
      const parsed = JSON.parse(raw);
      return buildWorkflowViewerStepCards(parsed?.steps);
    } catch {
      return [];
    }
  }

  jumpToJsonStep(stepIndex: number): void {
    this.viewMode = 'json';
    this.cdr.detectChanges();
    const raw = this.form.controls.definitionJson.value ?? '';
    const pos = findCaretIndexForWorkflowStep(raw, stepIndex);
    queueMicrotask(() => {
      setTimeout(() => {
        const el = this.definitionJsonEl?.nativeElement;
        if (!el) return;
        el.focus();
        if (pos >= 0 && pos < el.value.length) {
          el.setSelectionRange(pos, pos);
        }
      }, 0);
    });
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
      this.scheduleEnabled = !!this.workflow.scheduleEnabled;
      this.scheduleCron = this.workflow.scheduleCron ?? '';

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
      this.scheduleEnabled = !!this.workflow.scheduleEnabled;
      this.scheduleCron = this.workflow.scheduleCron ?? '';
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to save workflow.';
      this.errorDetails = Array.isArray(e?.error?.details) ? (e.error.details as ApiErrorDetail[]) : [];
    } finally {
      this.saving = false;
    }
  }

  async saveSchedule(): Promise<void> {
    if (!this.workflow) return;

    this.scheduleSaving = true;
    this.error = null;
    this.errorDetails = [];

    try {
      const id = this.workflowId;
      const req = { enabled: this.scheduleEnabled, cron: this.scheduleCron.trim() || null };
      this.workflow = await firstValueFrom(
        this.http.put<WorkflowDefinitionDetailsDto>(`/api/workflows/${id}/schedule`, req),
      );
      this.scheduleEnabled = !!this.workflow.scheduleEnabled;
      this.scheduleCron = this.workflow.scheduleCron ?? '';
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to save schedule.';
      this.errorDetails = Array.isArray(e?.error?.details) ? (e.error.details as ApiErrorDetail[]) : [];
    } finally {
      this.scheduleSaving = false;
    }
  }

  async exportPack(): Promise<void> {
    if (!this.workflow) return;

    this.exporting = true;
    this.exportError = null;

    try {
      type WorkflowExportPack = {
        exportFormatVersion: number;
        name: string;
        definitionJson: string;
        exportedAtUtc: string;
        sourceWorkflowDefinitionId: string;
      };

      const pack = await firstValueFrom(
        this.http.get<WorkflowExportPack>(`/api/workflows/${this.workflow.workflowDefinitionId}/export`),
      );
      const text = JSON.stringify(pack, null, 2);
      const blob = new Blob([text], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const safe = String(pack.name ?? 'workflow')
        .replace(/[^\w\-]+/g, '_')
        .replace(/^_+|_+$/g, '')
        .slice(0, 80);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${safe || 'workflow'}-export.json`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e: any) {
      this.exportError = e?.error?.message ?? e?.message ?? 'Export failed.';
    } finally {
      this.exporting = false;
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
