import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component')
      .then((component) => component.DashboardComponent),
    title: 'LiveOps status',
  },
  {
    path: 'audit',
    loadComponent: () => import('./features/audit/audit.component')
      .then((component) => component.AuditComponent),
    title: 'LiveOps audit',
  },
  {
    path: 'players',
    loadComponent: () => import('./features/players/player-workspace.component')
      .then((component) => component.PlayerWorkspaceComponent),
    title: 'LiveOps players',
  },
  {
    path: 'players/:characterId',
    loadComponent: () => import('./features/players/player-workspace.component')
      .then((component) => component.PlayerWorkspaceComponent),
    title: 'LiveOps player',
  },
  {
    path: 'account-risk',
    loadComponent: () => import('./features/account-risk/account-risk.component')
      .then((component) => component.AccountRiskComponent),
    title: 'LiveOps direct-transfer review',
  },
  {
    path: 'account-risk/:accountId',
    loadComponent: () => import('./features/account-risk/account-risk-detail.component')
      .then((component) => component.AccountRiskDetailComponent),
    title: 'LiveOps account investigation',
  },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' },
];
