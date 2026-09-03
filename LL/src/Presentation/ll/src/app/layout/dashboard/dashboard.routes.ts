import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard.component';
import { CombatComponent } from '../../shared/components/combat/combat.component';
import { GUIDE_PAGE_IDS } from '../../shared/help/guide-catalog';
import { focusedBetaUnavailableMatchGuard } from '../../core/guards/focused-beta-journey.guard';

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
        data: { guidePageId: GUIDE_PAGE_IDS.combat },
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
        canMatch: [focusedBetaUnavailableMatchGuard],
        loadChildren: () =>
          import('./../../features/game/city/city.routes').then(
            (m) => m.CITY_ROUTES,
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
        canMatch: [focusedBetaUnavailableMatchGuard],
        loadChildren: () =>
          import('./../../features/game/prophecies/prophecies.routes').then(
            (m) => m.PROPHECIES_ROUTES,
          ),
      },
      {
        path: 'quests',
        loadChildren: () =>
          import('./../../features/game/quests/quests.routes').then(
            (m) => m.QUESTS_ROUTES,
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
