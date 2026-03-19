import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterLink, RouterOutlet],
  template: `
    <header style="padding: 12px 16px; border-bottom: 1px solid #eee;">
      <nav style="display:flex; gap: 12px; flex-wrap: wrap; align-items: baseline;">
        <a routerLink="/upgrade">Upgrade</a>
        <span style="color:#999;">|</span>
        <a routerLink="/lowcode/auth">Low-code Auth</a>
        <a routerLink="/lowcode/admin/tenants">Admin / Tenants</a>
        <a routerLink="/lowcode/workflows">Workflows</a>
        <a routerLink="/lowcode/entities">Entities</a>
      </nav>
    </header>

    <router-outlet />
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'frontend';
}
