import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard.component';
import { CreaturesComponent } from '../../features/creatures/creatures.component';
import { ItemsComponent } from '../../features/items/items.component';

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
    ],
  },
];
