import { Routes } from '@angular/router';
import { WorkspaceRoute } from './shell/workspace-route';

export const routes: Routes = [
  { path: '', component: WorkspaceRoute, title: 'Nodra' },
  { path: 'workspace/:vaultId/:module', component: WorkspaceRoute, title: 'Nodra Workspace' },
  { path: '**', redirectTo: '' },
];
