import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

type WorkflowDefinitionDetailsDto = {
  workflowDefinitionId: string;
  name: string;
  definitionJson: string;
  createdAtUtc: string;
  updatedAtUtc: string;
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

      <section style="margin-top: 12px; padding: 12px; border: 1px solid #ddd; border-radius: 8px;">
        <div style="font-weight: 600; margin-bottom: 8px;">Templates (executable)</div>
        <div style="display:flex; gap: 8px; flex-wrap: wrap;">
          <button type="button" (click)="applyTemplate('noop')">No-op</button>
          <button type="button" (click)="applyTemplate('delay250')">Delay 250ms</button>
          <button type="button" (click)="applyTemplate('delay3')">3x Delay</button>
          <button type="button" (click)="applyTemplate('set')">Set (seed output)</button>
          <button type="button" (click)="applyTemplate('map')">Map (projection)</button>
          <button type="button" (click)="applyTemplate('merge')">Merge (combine objects)</button>
          <button type="button" (click)="applyTemplate('foreach')">Foreach (iterate)</button>
          <button type="button" (click)="applyTemplate('require')">Require (guard)</button>
          <button type="button" (click)="applyTemplate('domainEcho')">Domain: echo</button>
          <button type="button" (click)="applyTemplate('domainCreateRecord')">Domain: create record</button>
          <button type="button" (click)="applyTemplate('domainUpdateRecord')">Domain: update record</button>
          <button type="button" (click)="applyTemplate('domainDeleteRecord')">Domain: delete record</button>
          <button type="button" (click)="applyTemplate('domainUpsertRecord')">Domain: upsert record</button>
          <button type="button" (click)="applyTemplate('setAndUpdateById')">Set + updateById (context var)</button>
        </div>
        <div style="margin-top: 6px; color:#444;">These templates only use step types currently supported by the engine: <code>noop</code>, <code>delay</code>, <code>set</code>, <code>map</code>, <code>merge</code>, <code>foreach</code>, <code>require</code>, <code>domainCommand</code>.</div>
      </section>

      <form [formGroup]="form" style="margin-top: 12px; display:flex; flex-direction: column; gap: 12px;">
        <label>
          Name
          <input formControlName="name" placeholder="wf-demo" style="width: 100%; max-width: 520px;" />
        </label>

        <label>
          Definition JSON
          <textarea formControlName="definitionJson" rows="14" style="width: 100%; font-family: monospace;"></textarea>
        </label>
      </form>
    </main>
  `,
})
export class LowCodeWorkflowNewPageComponent {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  creating = false;
  error: string | null = null;

  form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    definitionJson: new FormControl('{"steps":[{"type":"noop"},{"type":"delay","ms":250}]}' , { nonNullable: true, validators: [Validators.required] }),
  });

  applyTemplate(
    kind:
      | 'noop'
      | 'delay250'
      | 'delay3'
      | 'set'
      | 'map'
      | 'merge'
      | 'foreach'
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

  async create(): Promise<void> {
    this.creating = true;
    this.error = null;

    try {
      const req = {
        name: this.form.controls.name.value.trim(),
        definitionJson: this.form.controls.definitionJson.value.trim(),
      };

      const created = await firstValueFrom(this.http.post<WorkflowDefinitionDetailsDto>('/api/workflows', req));
      await this.router.navigate(['/lowcode/workflows', created.workflowDefinitionId]);
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to create workflow.';
    } finally {
      this.creating = false;
    }
  }
}
