import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard.component';
import { CreaturesComponent } from '../../features/creatures/creatures.component';
import { ItemsComponent } from '../../features/items/items.component';
import { RecipesComponent } from '../../features/recipes/recipes.component';
import { CombatDiagnosticsComponent } from '../../features/diagnostics/combat-diagnostics.component';

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
        path: 'recipes',
        component: RecipesComponent,
      },
      {
        path: 'diagnostics',
        component: CombatDiagnosticsComponent,
      },
    ],
  },
];
