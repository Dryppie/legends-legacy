import { Routes } from '@angular/router';
import { ProfessionsComponent } from './professions.component';
import { GatheringComponent } from './gathering/gathering.component';
import { CraftingComponent } from './crafting/crafting.component';

export const PROFESSIONS_ROUTES: Routes = [
  {
    path: '',
    component: ProfessionsComponent,
    children: [
      {
        path: '',
        redirectTo: 'gathering',
        pathMatch: 'full',
      },
      {
        path: 'gathering',
        component: GatheringComponent,
      },
      {
        path: 'crafting/:id',
        component: CraftingComponent,
      },
    ],
  },
];
