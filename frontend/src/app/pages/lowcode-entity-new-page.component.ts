import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

type EntityDefinitionDetailsDto = {
  entityDefinitionId: string;
  name: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  fields: unknown[];
};

@Component({
  selector: 'app-lowcode-entity-new-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <main style="padding: 16px; max-width: 980px; margin: 0 auto;">
      <div style="display:flex; gap: 12px; align-items: center; flex-wrap: wrap;">
        <a routerLink="/lowcode/entities">← Entities</a>
        <h2 style="margin:0;">New entity</h2>
        <button type="button" (click)="create()" [disabled]="form.invalid || creating">Create</button>
        <div *ngIf="creating">Creating...</div>
        <div *ngIf="error" style="color:#b00020;">{{ error }}</div>
      </div>

      <form [formGroup]="form" style="margin-top: 12px; display:flex; flex-direction: column; gap: 12px;">
        <label>
          Name
          <input formControlName="name" placeholder="Vendor" style="width: 100%; max-width: 520px;" />
        </label>
      </form>
    </main>
  `,
})
export class LowCodeEntityNewPageComponent {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  creating = false;
  error: string | null = null;

  form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  async create(): Promise<void> {
    this.creating = true;
    this.error = null;

    try {
      const req = { name: this.form.controls.name.value.trim() };
      const created = await firstValueFrom(this.http.post<EntityDefinitionDetailsDto>('/api/entities', req));
      await this.router.navigate(['/lowcode/entities', created.entityDefinitionId]);
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to create entity.';
    } finally {
      this.creating = false;
    }
  }
}
