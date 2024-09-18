import { Routes } from '@angular/router';

export const PUBLIC_ROUTES: Routes = [
  {
    path: '',
    loadChildren: () =>
      import('./landing/landing.routes').then((m) => m.LANDING_ROUTES),
  },
];
