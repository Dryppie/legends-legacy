import { Routes } from '@angular/router';
import { ProfessionsComponent } from './professions.component';
import { GatheringComponent } from './gathering/gathering.component';

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
        path: ':id',
        component: GatheringComponent,
      },
    ],
  },
];
