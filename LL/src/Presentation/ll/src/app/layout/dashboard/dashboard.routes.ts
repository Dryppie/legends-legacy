import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard.component';
import { CombatComponent } from '../../shared/components/combat/combat.component';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: DashboardComponent,
    children: [
      {
        path: '',
        redirectTo: 'character',
        pathMatch: 'full',
      },
      {
        path: 'combat',
        component: CombatComponent,
      },
      {
        path: 'character',
        loadChildren: () =>
          import('./../../features/game/character/character.routes').then(
            (m) => m.CHARACTER_ROUTES,
          ),
      },
      {
        path: 'city',
        loadChildren: () =>
          import('./../../features/game/city/city.routes').then(
            (m) => m.CITY_ROUTES,
          ),
      },
      {
        path: 'professions',
        loadChildren: () =>
          import('./../../features/game/professions/professions.routes').then(
            (m) => m.PROFESSIONS_ROUTES,
          ),
      },
      {
        path: 'world',
        loadChildren: () =>
          import('./../../features/game/world/world.routes').then(
            (m) => m.WORLD_ROUTES,
          ),
      },
      {
        path: 'prophecies',
        loadChildren: () =>
          import('./../../features/game/prophecies/prophecies.routes').then(
            (m) => m.PROPHECIES_ROUTES,
          ),
      },
      {
        path: 'settings',
        loadChildren: () =>
          import('./../../features/game/settings/settings.routes').then(
            (m) => m.SETTINGS_ROUTES,
          ),
      },
    ],
  },
];
