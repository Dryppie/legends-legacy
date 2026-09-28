import { Routes, CanDeactivateFn } from '@angular/router';

const finishSubmission: CanDeactivateFn<{ previewSubmitting?: boolean; busyAction?: string; saving?: boolean }> = component =>
  !component.previewSubmitting && !component.busyAction && !component.saving;

export const routes: Routes = [
  { path: 'cases', canDeactivate: [finishSubmission], loadComponent: () => import('./features/cases/cases.component').then(c => c.CasesComponent), title: 'Support cases' },
  { path: 'cases/:caseId', canDeactivate: [finishSubmission], loadComponent: () => import('./features/cases/cases.component').then(c => c.CasesComponent), title: 'Support case' },
  {
    path: 'analytics',
    loadComponent: () => import('./features/analytics/analytics.component')
      .then((component) => component.AnalyticsComponent),
    title: 'LiveOps analytics',
  },
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
    path: 'players', canDeactivate: [finishSubmission],
    loadComponent: () => import('./features/players/player-workspace.component')
      .then((component) => component.PlayerWorkspaceComponent),
    title: 'LiveOps players',
  },
  {
    path: 'players/:characterId', canDeactivate: [finishSubmission],
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
    path: 'account-risk/:accountId', canDeactivate: [finishSubmission],
    loadComponent: () => import('./features/account-risk/account-risk-detail.component')
      .then((component) => component.AccountRiskDetailComponent),
    title: 'LiveOps account investigation',
  },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' },
];
