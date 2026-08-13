import { Routes } from '@angular/router';
import { WorldComponent } from './world.component';
import { RegionComponent } from './region/region.component';
import { DungeonPageComponent } from './region/dungeons/dungeon-page/dungeon-page.component';
import { GUIDE_PAGE_IDS } from '../../../shared/help/guide-catalog';
import { TowerOverviewComponent } from './tower/overview/tower-overview.component';
import { TowerRallyComponent } from './tower/rally/tower-rally.component';
import { TowerHallOfFameComponent } from './tower/hall-of-fame/tower-hall-of-fame.component';
import { TowerPersonalExpeditionsComponent } from './tower/personal-expeditions/tower-personal-expeditions.component';

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
        path: 'tower/expeditions/:rallyId',
        component: TowerRallyComponent,
      },
      {
        path: 'tower/rallies/:rallyId',
        redirectTo: 'tower/expeditions/:rallyId',
        pathMatch: 'full',
      },
      {
        path: 'tower/hall-of-fame',
        component: TowerHallOfFameComponent,
      },
      {
        path: 'tower/personal-expeditions',
        component: TowerPersonalExpeditionsComponent,
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
