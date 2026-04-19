import { Routes } from '@angular/router';
import { WorldComponent } from './world.component';
import { RegionComponent } from './region/region.component';
import { DungeonPageComponent } from './region/dungeons/dungeon-page/dungeon-page.component';

export const WORLD_ROUTES: Routes = [
  {
    path: '',
    component: WorldComponent,
    children: [
      {
        path: 'dungeon',
        component: DungeonPageComponent,
      },
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
