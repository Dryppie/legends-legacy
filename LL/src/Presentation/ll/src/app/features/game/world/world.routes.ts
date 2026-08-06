import { Routes } from '@angular/router';
import { WorldComponent } from './world.component';
import { RegionComponent } from './region/region.component';
import { DungeonPageComponent } from './region/dungeons/dungeon-page/dungeon-page.component';
import { GUIDE_PAGE_IDS } from '../../../shared/help/guide-catalog';

export const WORLD_ROUTES: Routes = [
  {
    path: '',
    component: WorldComponent,
    children: [
      {
        path: 'dungeon',
        component: DungeonPageComponent,
        data: { guidePageId: GUIDE_PAGE_IDS.dungeons },
      },
      {
        path: '',
        redirectTo: 'shenic',
        pathMatch: 'full',
      },
      {
        path: ':id',
        component: RegionComponent,
        data: { guidePageId: GUIDE_PAGE_IDS.world },
      },
    ],
  },
];
