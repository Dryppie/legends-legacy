import { Routes } from '@angular/router';
import { ProfessionsComponent } from './professions.component';
import { CraftingComponent } from './crafting/crafting.component';
import { GUIDE_PAGE_IDS } from '../../../shared/help/guide-catalog';

export const PROFESSIONS_ROUTES: Routes = [
  {
    path: '',
    component: ProfessionsComponent,
    children: [
      {
        path: '',
        redirectTo: 'crafting',
        pathMatch: 'full',
      },
      {
        path: 'crafting',
        component: CraftingComponent,
        data: { guidePageId: GUIDE_PAGE_IDS.crafting },
      },
      {
        path: 'crafting/:id',
        component: CraftingComponent,
        data: { guidePageId: GUIDE_PAGE_IDS.crafting },
      },
    ],
  },
];
