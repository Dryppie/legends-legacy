import { Routes } from '@angular/router';
import { WorldComponent } from './world.component';
import { RegionComponent } from './region/region.component';

export const WORLD_ROUTES: Routes = [
  {
    path: '',
    component: WorldComponent,
    children: [
      {
        path: '',
        redirectTo: 'shenic',
        pathMatch: 'full',
      },
      {
        path: ':id',
        component: RegionComponent,
      },
    ],
  },
];
