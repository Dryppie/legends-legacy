import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard.component';
import { RegionsComponent } from '../../features/regions/regions.component';
import { CreaturesComponent } from '../../features/creatures/creatures.component';
import { MainLayoutComponent } from '../main-layout/main-layout.component';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: DashboardComponent,
    children: [
      {
        path: '',
        component: MainLayoutComponent,
      },
      {
        path: 'creatures',
        component: CreaturesComponent,
      },
      {
        path: 'regions',
        component: RegionsComponent,
      },
    ],
  },
];
