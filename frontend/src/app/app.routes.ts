import { Routes } from '@angular/router';

import { UpgradePageComponent } from './pages/upgrade-page.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'upgrade' },
  { path: 'upgrade', component: UpgradePageComponent },
];
