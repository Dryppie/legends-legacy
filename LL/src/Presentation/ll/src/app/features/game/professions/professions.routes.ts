import { Routes } from '@angular/router';
import { ProfessionsComponent } from './professions.component';
import { WoodcuttingComponent } from './woodcutting/woodcutting.component';

export const PROFESSIONS_ROUTES: Routes = [
  {
    path: '',
    component: ProfessionsComponent,
    children: [
      {
        path: '',
        redirectTo: 'woodcutting',
        pathMatch: 'full',
      },
      {
        path: 'woodcutting',
        component: WoodcuttingComponent,
      },
    ],
  },
];
