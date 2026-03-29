import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ChangeDetectorRef, Component, ElementRef, inject, ViewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { buildMergedContextVarSuggestions } from './lowcode-workflow-context-suggestions';
import { groupLintWarningsByCode, type LintWarningGroup } from './lowcode-workflow-lint-utils';
import { minifyWorkflowDefinitionJson, prettifyWorkflowDefinitionJson } from './lowcode-workflow-json-form';
import {
  filterWorkflowNewTemplateEntries,
  WORKFLOW_NEW_TEMPLATE_ENTRIES,
  type WorkflowNewTemplateEntry,
} from './lowcode-workflow-new-template-entries';
import {
  appendBuilderStep,
  moveBuilderStep,
  moveBuilderStepToSlot,
  parseBuilderStepSummaries,
  removeBuilderStepAt,
  WORKFLOW_BUILDER_PALETTE,
  type BuilderStepType,
} from './lowcode-workflow-builder-utils';
import { findCaretIndexForWorkflowStep } from './lowcode-workflow-viewer-utils';

type WorkflowDefinitionDetailsDto = {
  workflowDefinitionId: string;
  name: string;
  definitionJson: string;
  lintWarnings: { code: string; message: string }[];
  inboundTriggerConfigured: boolean;
  scheduleEnabled?: boolean;
  scheduleCron?: string | null;
  scheduleNextDueUtc?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

type ApiErrorDetail = {
  path?: string | null;
  code: string;
  message: string;
  severity?: string | null;
};

@Component({
  selector: 'app-lowcode-workflow-new-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <main style="padding: 16px; max-width: 980px; margin: 0 auto;">
      <div style="display:flex; gap: 12px; align-items: center; flex-wrap: wrap;">
        <a routerLink="/lowcode/workflows">← Workflows</a>
        <h2 style="margin:0;">New workflow</h2>
        <span style="color:#999;">|</span>
        <button type="button" (click)="viewMode = 'builder'" [disabled]="viewMode === 'builder'">Builder</button>
        <button type="button" (click)="viewMode = 'json'" [disabled]="viewMode === 'json'">JSON</button>
        <button type="button" (click)="create()" [disabled]="form.invalid || creating">Create</button>
        <div *ngIf="creating">Creating...</div>
        <div *ngIf="error" style="color:#b00020;">{{ error }}</div>
      </div>
      <section *ngIf="errorDetails.length > 0" style="margin-top: 10px; padding: 10px 12px; border: 1px solid #f3d5d5; border-radius: 8px; background: #fff7f7;">
        <div style="font-weight: 600; margin-bottom: 6px;">Validation details</div>
        <div *ngFor="let d of errorDetails" style="font-family: monospace; font-size: 12px; color:#8a1f1f; margin-bottom: 4px;">
          {{ d.path || '$' }} | {{ d.code }} | {{ d.message }}
        </div>
      </section>

      <section style="margin-top: 12px; padding: 12px; border: 1px solid #ddd; border-radius: 8px;">
        <div style="font-weight: 600; margin-bottom: 8px;">Templates (executable)</div>
        <label style="display:flex; flex-wrap: wrap; gap: 8px; align-items: center; margin-bottom: 8px;">
          <span style="font-size: 14px; font-weight: 500;">Filter</span>
          <input
            type="search"
            placeholder="step type, domain command, label…"
            [value]="templateFilter"
            (input)="templateFilter = $any($event.target).value"
            style="min-width: 220px; flex: 1; max-width: 420px;"
          />
        </label>
        <div style="display:flex; gap: 8px; flex-wrap: wrap;">
          <button type="button" *ngFor="let t of filteredTemplateEntries" (click)="applyTemplateEntry(t)">{{ t.label }}</button>
        </div>
        <div *ngIf="filteredTemplateEntries.length === 0" style="margin-top: 6px; color:#888;">No templates match filter.</div>
        <div style="margin-top: 6px; color:#444;">These templates only use step types currently supported by the engine: <code>noop</code>, <code>delay</code>, <code>set</code>, <code>map</code>, <code>merge</code>, <code>foreach</code>, <code>switch</code>, <code>require</code>, <code>domainCommand</code>.</div>
      </section>

      <form [formGroup]="form" style="margin-top: 12px; display:flex; flex-direction: column; gap: 12px;">
        <label>
          Name
          <input formControlName="name" placeholder="wf-demo" style="width: 100%; max-width: 520px;" />
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

        <section *ngIf="created?.lintWarnings?.length" style="padding: 10px 12px; border: 1px solid #f0e0a0; border-radius: 8px; background: #fffaf0;">
          <div style="display:flex; flex-wrap: wrap; gap: 8px 16px; align-items: baseline; margin-bottom: 6px;">
            <div style="font-weight: 600;">Lint warnings</div>
            <span style="font-size: 12px; padding: 2px 8px; border-radius: 999px; background:#fff4cc; color:#6b4e00;">
              {{ created?.lintWarnings?.length }} total
            </span>
          </div>
          <div *ngFor="let g of createdLintGrouped" style="margin-top: 8px;">
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
      </form>

      <section *ngIf="viewMode === 'builder'" style="margin-top: 12px;">
        <div *ngIf="viewerError" style="color:#b00020;">{{ viewerError }}</div>
        <div *ngIf="jsonFormatError && !viewerError" style="color:#b00020;">{{ jsonFormatError }}</div>
        <ng-container *ngIf="!viewerError">
          <p style="font-size: 13px; color:#555; margin: 0 0 12px 0; line-height: 1.45;">
            Steps run top to bottom. Runtime keys are <code>000</code>, <code>001</code>, … by order.
            Drag the <strong>⋮⋮</strong> handle to reorder, or use <strong>↑↓</strong> (preferred on touch — native HTML5 drag is pointer-first).
            Reordering or adding steps can break references to other steps — switch to JSON to adjust.
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
              (dragover)="onBuilderRowDragOver($event, row.index)"
              (dragleave)="onBuilderRowDragLeave($event, row.index)"
              (drop)="onBuilderRowDrop($event, row.index)"
              [style.outline]="builderDragHighlightRow === row.index ? '2px solid #0b5394' : 'none'"
              [style.outline-offset]="'2px'"
              style="display:flex; align-items:center; gap: 10px; flex-wrap: wrap; padding: 10px 12px; border: 1px solid #ddd; border-radius: 8px; background: #fafafa;"
            >
              <span
                draggable="true"
                title="Drag to reorder (pointer); on touch use ↑↓"
                (dragstart)="onBuilderRowDragStart($event, row.index)"
                (dragend)="onBuilderRowDragEnd()"
                style="cursor: grab; user-select: none; -webkit-user-select: none; touch-action: none; color: #555; font-size: 18px; line-height: 1; min-width: 44px; min-height: 44px; display: inline-flex; align-items: center; justify-content: center; box-sizing: border-box; border: 1px dashed #ccc; border-radius: 6px; background: #f0f0f0;"
                aria-label="Drag to reorder step"
                >⋮⋮</span>
              <span style="font-family: monospace; color:#666;">{{ formatBuilderStepKey(row.index) }}</span>
              <span style="font-family: monospace;"><b>{{ row.type }}</b></span>
              <span style="flex:1"></span>
              <button type="button" (click)="moveBuilderStepUp(row.index)" [disabled]="row.index === 0" style="min-width: 44px; min-height: 44px;">↑</button>
              <button type="button" (click)="moveBuilderStepDown(row.index)" [disabled]="row.index === builderStepRows.length - 1" style="min-width: 44px; min-height: 44px;">↓</button>
              <button type="button" (click)="removeBuilderStep(row.index)" style="color:#b00020;">Remove</button>
              <button type="button" (click)="jumpToJsonStep(row.index)">JSON →</button>
            </div>
          </div>
        </ng-container>
      </section>
    </main>
  `,
})
export class LowCodeWorkflowNewPageComponent {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  @ViewChild('definitionJsonEl') definitionJsonEl?: ElementRef<HTMLTextAreaElement>;

  readonly contextVarDatalistId = 'wf-ctx-suggestions-new';

  viewMode: 'builder' | 'json' = 'json';

  readonly builderPalette = WORKFLOW_BUILDER_PALETTE;

  builderDragHighlightRow: number | null = null;

  templateFilter = '';
  jsonFormatError: string | null = null;

  creating = false;
  error: string | null = null;
  errorDetails: ApiErrorDetail[] = [];
  lintWarnings: { code: string; message: string }[] = [];
  created: WorkflowDefinitionDetailsDto | null = null;

  form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    definitionJson: new FormControl('{"steps":[{"type":"noop"},{"type":"delay","ms":250}]}' , { nonNullable: true, validators: [Validators.required] }),
  });

  get createdLintGrouped(): LintWarningGroup[] {
    return groupLintWarningsByCode(this.created?.lintWarnings);
  }

  get filteredTemplateEntries(): WorkflowNewTemplateEntry[] {
    return filterWorkflowNewTemplateEntries(WORKFLOW_NEW_TEMPLATE_ENTRIES, this.templateFilter);
  }

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

  onBuilderRowDragStart(ev: DragEvent, fromIndex: number): void {
    ev.dataTransfer?.setData('text/plain', String(fromIndex));
    if (ev.dataTransfer) ev.dataTransfer.effectAllowed = 'move';
  }

  onBuilderRowDragOver(ev: DragEvent, rowIndex: number): void {
    ev.preventDefault();
    if (ev.dataTransfer) ev.dataTransfer.dropEffect = 'move';
    this.builderDragHighlightRow = rowIndex;
  }

  onBuilderRowDragLeave(ev: DragEvent, rowIndex: number): void {
    const next = ev.relatedTarget as Node | null;
    if (next && (ev.currentTarget as HTMLElement).contains(next)) return;
    if (this.builderDragHighlightRow === rowIndex) this.builderDragHighlightRow = null;
  }

  onBuilderRowDrop(ev: DragEvent, rowIndex: number): void {
    ev.preventDefault();
    this.builderDragHighlightRow = null;
    const fromStr = ev.dataTransfer?.getData('text/plain') ?? '';
    const from = Number.parseInt(fromStr, 10);
    const n = this.builderStepRows.length;
    if (!Number.isFinite(from) || from < 0 || from >= n) return;
    const rowEl = ev.currentTarget as HTMLElement;
    const r = rowEl.getBoundingClientRect();
    const before = ev.clientY < r.top + r.height / 2;
    const targetSlot = before ? rowIndex : rowIndex + 1;
    this.applyBuilderMoveToSlot(from, targetSlot, n);
  }

  onBuilderRowDragEnd(): void {
    this.builderDragHighlightRow = null;
  }

  private applyBuilderMoveToSlot(from: number, targetSlot: number, stepCount: number): void {
    this.jsonFormatError = null;
    try {
      const raw = String(this.form.controls.definitionJson.value ?? '');
      this.form.controls.definitionJson.setValue(moveBuilderStepToSlot(raw, from, targetSlot, stepCount));
    } catch (e: any) {
      this.jsonFormatError = e?.message ?? 'Invalid JSON.';
    }
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

  get contextVarSuggestions(): string[] {
    return buildMergedContextVarSuggestions(String(this.form.controls.definitionJson.value ?? ''));
  }

  applyTemplateEntry(t: WorkflowNewTemplateEntry): void {
    this.form.controls.name.setValue(t.name);
    this.form.controls.definitionJson.setValue(t.json);
    this.jsonFormatError = null;
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

  validateContextVarSyntax(value: string): string | null {
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

  async create(): Promise<void> {
    const preflight = this.validateContextVarSyntax(this.form.controls.definitionJson.value ?? '');
    if (preflight) {
      this.error = preflight;
      return;
    }

    this.creating = true;
    this.error = null;
    this.errorDetails = [];
    this.lintWarnings = [];

    try {
      const req = {
        name: this.form.controls.name.value.trim(),
        definitionJson: this.form.controls.definitionJson.value.trim(),
      };

      const payload = await firstValueFrom(
        this.http.post<WorkflowDefinitionDetailsDto>('/api/workflows', req)
      );

      this.created = payload;

      if (payload.lintWarnings) {
        this.lintWarnings = payload.lintWarnings;
      }

      await this.router.navigate(['/lowcode/workflows', payload.workflowDefinitionId]);
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to create workflow.';
      this.errorDetails = Array.isArray(e?.error?.details) ? (e.error.details as ApiErrorDetail[]) : [];
    } finally {
      this.creating = false;
    }
  }
}
