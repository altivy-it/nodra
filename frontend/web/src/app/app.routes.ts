import { Routes } from '@angular/router';
import { WorkspaceRoute } from './shell/workspace-route';

export const routes: Routes = [
	{ path: '', component: WorkspaceRoute, title: 'Altivy Notes' },
	{ path: 'workspace/:vaultId/:module', component: WorkspaceRoute, title: 'Altivy Notes Workspace' },
	{ path: '**', redirectTo: '' }
];
