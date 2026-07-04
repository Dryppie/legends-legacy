import { Routes } from '@angular/router';
import { ProfessionsComponent } from './professions.component';
import { CraftingComponent } from './crafting/crafting.component';

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
      },
      {
        path: 'crafting/:id',
        component: CraftingComponent,
      },
    ],
  },
];
