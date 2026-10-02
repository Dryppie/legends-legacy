import { Routes } from '@angular/router';
import { ShowcaseEntryPageComponent } from './showcase-entry-page.component';
import { ShowcaseHomeComponent } from './showcase-home.component';
import { ShowcaseShellComponent } from './showcase-shell.component';
import { ShowcaseStore } from './showcase.store';

/**
 * The Grimoire showcase, at /grimoire (development builds only).
 *
 * Production builds swap this file for showcase.routes.production.ts (angular.json → fileReplacements), so none of
 * the showcase is built into them; app.routes.ts also matches the path only in dev mode.
 */
export const GRIMOIRE_SHOWCASE_ROUTES: Routes = [
  {
    path: '',
    component: ShowcaseShellComponent,
    providers: [ShowcaseStore],
    children: [
      {
        path: '',
        component: ShowcaseHomeComponent,
        title: 'Grimoire showcase',
      },
      { path: ':tier/:entry', component: ShowcaseEntryPageComponent },
      { path: ':tier/:entry/:story', component: ShowcaseEntryPageComponent },
    ],
  },
];
