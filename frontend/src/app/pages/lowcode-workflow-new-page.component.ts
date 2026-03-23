import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { groupLintWarningsByCode, type LintWarningGroup } from './lowcode-workflow-lint-utils';

type WorkflowDefinitionDetailsDto = {
  workflowDefinitionId: string;
  name: string;
  definitionJson: string;
  lintWarnings: { code: string; message: string }[];
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
        <div style="display:flex; gap: 8px; flex-wrap: wrap;">
          <button type="button" (click)="applyTemplate('noop')">No-op</button>
          <button type="button" (click)="applyTemplate('delay250')">Delay 250ms</button>
          <button type="button" (click)="applyTemplate('delay3')">3x Delay</button>
          <button type="button" (click)="applyTemplate('timeoutDelay')">Timeout (delay)</button>
          <button type="button" (click)="applyTemplate('set')">Set (seed output)</button>
          <button type="button" (click)="applyTemplate('map')">Map (projection)</button>
          <button type="button" (click)="applyTemplate('merge')">Merge (combine objects)</button>
          <button type="button" (click)="applyTemplate('foreach')">Foreach (iterate)</button>
          <button type="button" (click)="applyTemplate('switch')">Switch (branch)</button>
          <button type="button" (click)="applyTemplate('retryUpdateById')">Retry (updateById)</button>
          <button type="button" (click)="applyTemplate('require')">Require (guard)</button>
          <button type="button" (click)="applyTemplate('domainEcho')">Domain: echo</button>
          <button type="button" (click)="applyTemplate('domainCreateRecord')">Domain: create record</button>
          <button type="button" (click)="applyTemplate('domainUpdateRecord')">Domain: update record</button>
          <button type="button" (click)="applyTemplate('domainDeleteRecord')">Domain: delete record</button>
          <button type="button" (click)="applyTemplate('domainUpsertRecord')">Domain: upsert record</button>
          <button type="button" (click)="applyTemplate('setAndUpdateById')">Set + updateById (context var)</button>
        </div>
        <div style="margin-top: 6px; color:#444;">These templates only use step types currently supported by the engine: <code>noop</code>, <code>delay</code>, <code>set</code>, <code>map</code>, <code>merge</code>, <code>foreach</code>, <code>switch</code>, <code>require</code>, <code>domainCommand</code>.</div>
      </section>

      <form [formGroup]="form" style="margin-top: 12px; display:flex; flex-direction: column; gap: 12px;">
        <label>
          Name
          <input formControlName="name" placeholder="wf-demo" style="width: 100%; max-width: 520px;" />
        </label>

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
    </main>
  `,
})
export class LowCodeWorkflowNewPageComponent {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

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

  applyTemplate(
    kind:
      | 'noop'
      | 'delay250'
      | 'delay3'
      | 'timeoutDelay'
      | 'set'
      | 'map'
      | 'merge'
      | 'foreach'
      | 'switch'
      | 'retryUpdateById'
      | 'require'
      | 'domainEcho'
      | 'domainCreateRecord'
      | 'domainUpdateRecord'
      | 'domainDeleteRecord'
      | 'domainUpsertRecord'
      | 'setAndUpdateById'
  ): void {
    const templates: Record<typeof kind, { name: string; json: string }> = {
      noop: {
        name: 'wf-noop',
        json: '{"steps":[{"type":"noop"}]}',
      },
      delay250: {
        name: 'wf-delay-250ms',
        json: '{"steps":[{"type":"delay","ms":250}]}',
      },
      delay3: {
        name: 'wf-3x-delay',
        json: '{"steps":[{"type":"delay","ms":100},{"type":"delay","ms":200},{"type":"delay","ms":300}]}',
      },
      timeoutDelay: {
        name: 'wf-timeout-delay',
        json: '{"steps":[{"type":"delay","ms":200,"timeoutMs":50},{"type":"noop"}]}',
      },
      set: {
        name: 'wf-set-seed',
        json: '{"steps":[{"type":"set","output":{"recordId":"<RECORD_ID_GUID>","note":"seeded value"}},{"type":"noop"}]}',
      },
      map: {
        name: 'wf-map-projection',
        json: '{"steps":[{"type":"domainCommand","command":"entityRecord.createByEntityName","entityName":"Company","data":{"name":"Acme Ltd","status":"active"}},{"type":"map","mappings":{"recordId":"000.entityRecordId"}},{"type":"noop"}]}',
      },
      merge: {
        name: 'wf-merge-objects',
        json: '{"steps":[{"type":"set","output":{"a":1,"b":2}},{"type":"merge","sources":[{"b":99,"c":3},"000"]},{"type":"noop"}]}',
      },
      foreach: {
        name: 'wf-foreach-items',
        json: '{"steps":[{"type":"set","output":{"items":[{"n":1},{"n":2}]}},{"type":"foreach","items":"000.items","do":{"type":"map","mappings":{"n":"item.n"}}},{"type":"noop"}]}',
      },
      switch: {
        name: 'wf-switch-branch',
        json: '{"steps":[{"type":"set","output":{"kind":"a"}},{"type":"switch","value":"000.kind","cases":[{"when":"a","do":{"type":"set","output":{"result":1}}},{"when":"b","do":{"type":"set","output":{"result":2}}}],"default":{"type":"set","output":{"result":99}}},{"type":"noop"}]}',
      },
      retryUpdateById: {
        name: 'wf-retry-update-by-id',
        json: '{"steps":[{"type":"domainCommand","command":"entityRecord.updateById","recordId":"<RECORD_ID_GUID>","data":{"status":"active"},"retry":{"maxAttempts":5,"delayMs":200,"backoffFactor":2,"maxDelayMs":2000}},{"type":"noop"}]}',
      },
      require: {
        name: 'wf-require-guard',
        json: '{"steps":[{"type":"domainCommand","command":"entityRecord.createByEntityName","entityName":"Company","data":{"name":"Acme Ltd","status":"active"}},{"type":"require","path":"000.entityRecordId"},{"type":"noop"}]}',
      },
      domainEcho: {
        name: 'wf-domain-echo',
        json: '{"steps":[{"type":"domainCommand","command":"echo"}]}',
      },
      domainCreateRecord: {
        name: 'wf-domain-create-record',
        json: '{"steps":[{"type":"domainCommand","command":"entityRecord.createByEntityName","entityName":"Company","data":{"name":"Acme Ltd","status":"active"}}]}',
      },
      domainUpdateRecord: {
        name: 'wf-domain-update-record',
        json: '{"steps":[{"type":"domainCommand","command":"entityRecord.updateById","recordId":"<RECORD_ID_GUID>","data":{"name":"Acme Updated","status":"inactive"}}]}',
      },
      domainDeleteRecord: {
        name: 'wf-domain-delete-record',
        json: '{"steps":[{"type":"domainCommand","command":"entityRecord.deleteById","recordId":"<RECORD_ID_GUID>"}]}',
      },
      domainUpsertRecord: {
        name: 'wf-domain-upsert-record',
        json: '{"steps":[{"type":"domainCommand","command":"entityRecord.upsertByEntityName","entityName":"Company","uniqueKey":"externalId","uniqueValue":"c-1","data":{"externalId":"c-1","name":"Acme Upsert","status":"active"}}]}',
      },
      setAndUpdateById: {
        name: 'wf-set-update-by-id',
        json: '{"steps":[{"type":"set","output":{"recordId":"<RECORD_ID_GUID>"}},{"type":"domainCommand","command":"entityRecord.updateById","recordId":"${000.recordId}","data":{"name":"Acme Updated","status":"inactive"}}]}',
      },
    };

    const t = templates[kind];
    this.form.controls.name.setValue(t.name);
    this.form.controls.definitionJson.setValue(t.json);
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
