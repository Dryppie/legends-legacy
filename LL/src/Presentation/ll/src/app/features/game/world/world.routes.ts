import { Routes } from '@angular/router';
import { WorldComponent } from './world.component';
import { RegionComponent } from './region/region.component';
import { DungeonPageComponent } from './region/dungeons/dungeon-page/dungeon-page.component';
import { GUIDE_PAGE_IDS } from '../../../shared/help/guide-catalog';
import { TowerOverviewComponent } from './tower/overview/tower-overview.component';
import { TowerRallyComponent } from './tower/rally/tower-rally.component';
import { TowerHallOfFameComponent } from './tower/hall-of-fame/tower-hall-of-fame.component';

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
        path: 'tower',
        component: TowerOverviewComponent,
      },
      {
        path: 'tower/rallies/:rallyId',
        component: TowerRallyComponent,
      },
      {
        path: 'tower/hall-of-fame',
        component: TowerHallOfFameComponent,
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
