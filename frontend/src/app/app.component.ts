import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

import { lowcodeTenantSlugForUi } from './lowcode/lowcode-session.store';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterLink, RouterOutlet],
  template: `
    <header style="padding: 12px 16px; border-bottom: 1px solid #eee;">
      <nav
        style="display:flex; gap: 12px; flex-wrap: wrap; align-items: center; justify-content: space-between; row-gap: 8px;"
      >
        <div style="display:flex; gap: 12px; flex-wrap: wrap; align-items: baseline;">
          <a routerLink="/lowcode/workflows">Workflows</a>
          <a routerLink="/lowcode/workflow-runs">Runs</a>
          <a routerLink="/lowcode/entities">Entities</a>
          <span style="color:#999;">|</span>
          <a routerLink="/lowcode/auth">Auth</a>
          <a routerLink="/lowcode/admin/tenants">Admin · Tenants</a>
          <span style="color:#999;">|</span>
          <a routerLink="/upgrade">Upgrade</a>
        </div>
        <div style="font-size: 13px; color: #444;">
          Tenant:
          <strong [style.color]="tenantSlug() ? '#111' : '#b45309'">{{ tenantSlug() || 'not set' }}</strong>
          <a routerLink="/lowcode/auth" style="margin-left: 10px;">Set</a>
        </div>
      </nav>
    </header>

    <router-outlet />
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  readonly tenantSlug = lowcodeTenantSlugForUi;
}
