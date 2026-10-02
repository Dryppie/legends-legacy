import { isDevMode } from '@angular/core';
import { Routes } from '@angular/router';
import { NotFoundPageComponent } from './features/error-pages/not-found-page/not-found-page.component';
import { publicGuard } from './core/guards/public/public.guard';
import { authGuard } from './core/guards/auth/auth.guard';
import { maintenanceGuard } from './core/guards/maintenance.guard';

export const routes: Routes = [
  {
    // The Grimoire showcase (ANGULAR_DESIGN_SYSTEM_PLAN.md, step 3): development builds only. Production builds also
    // replace its routes with none (angular.json → fileReplacements), so none of it ships.
    path: 'grimoire',
    loadChildren: () =>
      import('./grimoire/showcase/showcase.routes').then(
        (m) => m.GRIMOIRE_SHOWCASE_ROUTES,
      ),
    canMatch: [() => isDevMode()],
  },
  {
    path: '',
    loadChildren: () =>
      import('./features/public/public.routes').then((m) => m.PUBLIC_ROUTES),
    canActivate: [publicGuard],
  },
  {
    path: 'game',
    loadChildren: () =>
      import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
    canActivate: [maintenanceGuard, authGuard],
  },
  {
    path: '**',
    component: NotFoundPageComponent,
    canActivate: [maintenanceGuard],
  },
];
