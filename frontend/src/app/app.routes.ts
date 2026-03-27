import { Routes } from '@angular/router';

import { UpgradePageComponent } from './pages/upgrade-page.component';
import { LowCodeAuthPageComponent } from './pages/lowcode-auth-page.component';
import { LowCodeWorkflowsPageComponent } from './pages/lowcode-workflows-page.component';
import { LowCodeEntitiesPageComponent } from './pages/lowcode-entities-page.component';
import { LowCodeWorkflowDetailsPageComponent } from './pages/lowcode-workflow-details-page.component';
import { LowCodeRunDetailsPageComponent } from './pages/lowcode-run-details-page.component';
import { LowCodeWorkflowNewPageComponent } from './pages/lowcode-workflow-new-page.component';
import { LowCodeEntityNewPageComponent } from './pages/lowcode-entity-new-page.component';
import { LowCodeEntityDetailsPageComponent } from './pages/lowcode-entity-details-page.component';
import { LowCodeAdminTenantsPageComponent } from './pages/lowcode-admin-tenants-page.component';
import { LowCodeEntityRecordsPageComponent } from './pages/lowcode-entity-records-page.component';
import { LowCodeWorkflowRunsPageComponent } from './pages/lowcode-workflow-runs-page.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'upgrade' },
  { path: 'upgrade', component: UpgradePageComponent },
  { path: 'lowcode/auth', component: LowCodeAuthPageComponent },
  { path: 'lowcode/admin/tenants', component: LowCodeAdminTenantsPageComponent },
  { path: 'lowcode/workflows', component: LowCodeWorkflowsPageComponent },
  { path: 'lowcode/workflow-runs', component: LowCodeWorkflowRunsPageComponent },
  { path: 'lowcode/workflows/new', component: LowCodeWorkflowNewPageComponent },
  { path: 'lowcode/workflows/:id', component: LowCodeWorkflowDetailsPageComponent },
  { path: 'lowcode/entities', component: LowCodeEntitiesPageComponent },
  { path: 'lowcode/entities/new', component: LowCodeEntityNewPageComponent },
  { path: 'lowcode/entities/:id/records', component: LowCodeEntityRecordsPageComponent },
  { path: 'lowcode/entities/:id', component: LowCodeEntityDetailsPageComponent },
  { path: 'lowcode/runs/:runId', component: LowCodeRunDetailsPageComponent },
];
