import { Routes } from '@angular/router';
import { CharacterComponent } from './character.component';
import { InventoryComponent } from './inventory/inventory.component';
import { CharacterOverviewComponent } from './character-overview/character-overview.component';
import { SoulstoneArchiveComponent } from './soulstone-archive/soulstone-archive.component';
import { EssencesComponent } from './essences/essences.component';
import { AchievementsComponent } from './achievements/achievements.component';
import { GUIDE_PAGE_IDS } from '../../../shared/help/guide-catalog';

export const CHARACTER_ROUTES: Routes = [
  {
    path: '',
    component: CharacterComponent,
    children: [
      {
        path: '',
        redirectTo: 'character-overview',
        pathMatch: 'full',
      },
      {
        path: 'character-overview',
        component: CharacterOverviewComponent,
        data: { guidePageId: GUIDE_PAGE_IDS.characterOverview },
      },
      {
        path: 'inventory',
        component: InventoryComponent,
        data: { guidePageId: GUIDE_PAGE_IDS.inventory },
      },
      {
        path: 'essences',
        component: EssencesComponent,
        data: { guidePageId: GUIDE_PAGE_IDS.essences },
      },
      {
        path: 'achievements',
        component: AchievementsComponent,
        data: { guidePageId: GUIDE_PAGE_IDS.achievements },
      },
      {
        path: 'soulstone-archive',
        component: SoulstoneArchiveComponent,
        data: { guidePageId: GUIDE_PAGE_IDS.soulstones },
      },
    ],
  },
];
