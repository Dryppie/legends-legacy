import { Routes } from '@angular/router';
import { WorldComponent } from './world.component';
import { RegionComponent } from './region/region.component';
import { DungeonPageComponent } from './region/dungeons/dungeon-page/dungeon-page.component';
import { GUIDE_PAGE_IDS } from '../../../shared/help/guide-catalog';
import { TowerOverviewComponent } from './tower/overview/tower-overview.component';
import { TowerRallyComponent } from './tower/rally/tower-rally.component';
import { TowerHallOfFameComponent } from './tower/hall-of-fame/tower-hall-of-fame.component';
import { TowerPersonalExpeditionsComponent } from './tower/personal-expeditions/tower-personal-expeditions.component';
import { RaidPageComponent } from './raid/raid-page.component';
import { raidFeatureGuard } from '../../../core/guards/raid-feature.guard';
import { RegionBossComponent } from './region-boss/region-boss.component';
import { worldMapRegionRedirect } from '../../../core/guards/world-map-region.guard';

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
        path: 'raid/:raidId',
        component: RaidPageComponent,
        canActivate: [raidFeatureGuard],
        data: { guidePageId: GUIDE_PAGE_IDS.raids },
      },
      {
        path: 'tower',
        component: TowerOverviewComponent,
        data: { guideDisabled: true },
      },
      {
        path: 'tower/expeditions/:rallyId',
        component: TowerRallyComponent,
        data: { guideDisabled: true },
      },
      {
        path: 'tower/rallies/:rallyId',
        redirectTo: 'tower/expeditions/:rallyId',
        pathMatch: 'full',
      },
      {
        path: 'tower/hall-of-fame',
        component: TowerHallOfFameComponent,
        data: { guideDisabled: true },
      },
      {
        path: 'tower/personal-expeditions',
        component: TowerPersonalExpeditionsComponent,
        data: { guideDisabled: true },
      },
      {
        path: 'region-boss',
        component: RegionBossComponent,
        data: { guideDisabled: true },
      },
      {
        path: '',
        redirectTo: worldMapRegionRedirect,
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
