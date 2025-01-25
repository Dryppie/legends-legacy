import { Routes } from '@angular/router';
import { CharacterComponent } from './character.component';
import { InventoryComponent } from './inventory/inventory.component';
import { CharacterOverviewComponent } from './character-overview/character-overview.component';

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
      },
      {
        path: 'inventory',
        component: InventoryComponent,
      },
    ],
  },
];
