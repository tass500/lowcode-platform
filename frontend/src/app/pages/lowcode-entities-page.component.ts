import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

type EntityDefinitionListItemDto = {
  entityDefinitionId: string;
  name: string;
  createdAtUtc: string;
  updatedAtUtc: string;
};

type EntityDefinitionListResponse = {
  serverTimeUtc: string;
  items: EntityDefinitionListItemDto[];
};

@Component({
  selector: 'app-lowcode-entities-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <main style="padding: 16px; max-width: 980px; margin: 0 auto;">
      <div style="display:flex; gap: 12px; align-items: center; flex-wrap: wrap;">
        <h2 style="margin:0;">Entities</h2>
        <a routerLink="/lowcode/workflows">Workflows</a>
        <a routerLink="/lowcode/workflow-runs">All runs</a>
        <a routerLink="/lowcode/entities/new">New</a>
        <button type="button" (click)="load()" [disabled]="loading">Refresh</button>
        <div *ngIf="loading">Loading...</div>
        <div *ngIf="error" style="color:#b00020;">{{ error }}</div>
      </div>

      <div *ngIf="items.length > 0" style="margin-top: 14px; display: flex; flex-wrap: wrap; gap: 10px; align-items: center;">
        <label style="display: flex; align-items: center; gap: 8px; font-size: 14px; color: #333;">
          <span>Filter by name</span>
          <input
            type="search"
            [(ngModel)]="nameFilter"
            placeholder="Type to narrow the list…"
            autocomplete="off"
            style="min-width: 220px; padding: 6px 10px; box-sizing: border-box;"
          />
        </label>
        <span *ngIf="nameFilter.trim()" style="font-size: 13px; color: #666;">
          Showing {{ filteredItems.length }} of {{ items.length }}
        </span>
      </div>

      <div *ngIf="items.length === 0 && !loading" style="margin-top: 12px; color:#444;">
        No entity definitions yet. Create one with <b>New</b> or open <a routerLink="/lowcode/workflows">Workflows</a> first.
      </div>

      <div
        *ngIf="items.length > 0 && filteredItems.length === 0 && !loading"
        style="margin-top: 12px; color:#444;"
      >
        No entities match this filter. Clear the search or try another name.
      </div>

      <table *ngIf="filteredItems.length > 0" style="width:100%; border-collapse: collapse; margin-top: 12px;">
        <thead>
          <tr>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Name</th>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Updated</th>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Id</th>
            <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;"></th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let e of filteredItems">
            <td style="border-bottom:1px solid #eee; padding: 6px;">{{ e.name }}</td>
            <td style="border-bottom:1px solid #eee; padding: 6px;">{{ e.updatedAtUtc | date: 'medium' }}</td>
            <td style="border-bottom:1px solid #eee; padding: 6px; font-family: monospace;">{{ e.entityDefinitionId }}</td>
            <td style="border-bottom:1px solid #eee; padding: 6px;">
              <a [routerLink]="['/lowcode/entities', e.entityDefinitionId]">Open</a>
            </td>
          </tr>
        </tbody>
      </table>
    </main>
  `,
})
export class LowCodeEntitiesPageComponent implements OnInit {
  private readonly http = inject(HttpClient);

  items: EntityDefinitionListItemDto[] = [];
  nameFilter = '';
  loading = false;
  error: string | null = null;

  get filteredItems(): EntityDefinitionListItemDto[] {
    const q = this.nameFilter.trim().toLowerCase();
    if (!q) {
      return this.items;
    }
    return this.items.filter((e) => e.name.toLowerCase().includes(q));
  }

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const res = await firstValueFrom(this.http.get<EntityDefinitionListResponse>('/api/entities'));
      this.items = res.items ?? [];
    } catch (e: any) {
      this.error = e?.error?.message ?? e?.message ?? 'Failed to load entities.';
    } finally {
      this.loading = false;
    }
  }
}
