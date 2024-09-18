import { Routes } from '@angular/router';
import { NotFoundPageComponent } from './features/error-pages/not-found-page/not-found-page.component';
import { publicGuard } from './core/guards/public/public.guard';
import { authGuard } from './core/guards/auth/auth.guard';

export const routes: Routes = [
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
    canActivate: [authGuard],
  },
  {
    path: '**',
    component: NotFoundPageComponent,
  },
];
