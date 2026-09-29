import { Routes } from '@angular/router';
import { AnalyticsComponent } from './analytics.component';

export const ANALYTICS_ROUTES: Routes = [{
  path: '', component: AnalyticsComponent,
  children: [
    { path: '', pathMatch: 'full', redirectTo: 'activity' },
    { path: 'activity', data: { analyticsPage: 'activity' }, title: 'Activity & retention · LiveOps analytics', loadComponent: () => import('./activity-page.component').then(c => c.ActivityPageComponent) },
    { path: 'content', data: { analyticsPage: 'content' }, title: 'Content outcomes · LiveOps analytics', loadComponent: () => import('./content-page.component').then(c => c.ContentPageComponent) },
    { path: 'adoption', data: { analyticsPage: 'adoption' }, title: 'Adoption · LiveOps analytics', loadComponent: () => import('./adoption-page.component').then(c => c.AdoptionPageComponent) },
    { path: 'economy', data: { analyticsPage: 'economy' }, title: 'Economy · LiveOps analytics', loadComponent: () => import('./economy-page.component').then(c => c.EconomyPageComponent) },
    { path: 'itemization', data: { analyticsPage: 'itemization' }, title: 'Itemization · LiveOps analytics', loadComponent: () => import('./itemization-page.component').then(c => c.ItemizationPageComponent) },
    { path: '**', redirectTo: 'activity' },
  ],
}];
