import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard.component';
import { CreaturesComponent } from '../../features/creatures/creatures.component';
import { ItemsComponent } from '../../features/items/items.component';
import { CombatDiagnosticsComponent } from '../../features/diagnostics/combat-diagnostics.component';
import { EssenceCatalogComponent } from '../../features/essence-catalog/essence-catalog.component';
import { DungeonSimulatorComponent } from '../../features/diagnostics/dungeon-simulator.component';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: DashboardComponent,
    children: [
      {
        path: '',
        redirectTo: 'creatures',
        pathMatch: 'full',
      },
      {
        path: 'creatures',
        component: CreaturesComponent,
      },
      {
        path: 'items',
        component: ItemsComponent,
      },
      {
        path: 'diagnostics',
        component: CombatDiagnosticsComponent,
      },
      {
        path: 'dungeon-simulator',
        component: DungeonSimulatorComponent,
      },
      {
        path: 'essence-catalog',
        component: EssenceCatalogComponent,
      },
    ],
  },
];
