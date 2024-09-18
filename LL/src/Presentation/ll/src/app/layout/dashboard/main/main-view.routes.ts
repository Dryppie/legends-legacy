import { Routes } from '@angular/router';
import { MainViewComponent } from './main-view.component';

export const MAINVIEW_ROUTES: Routes = [
  {
    path: '',
    component: MainViewComponent,
    children: [
      {
        path: '',
        redirectTo: 'character',
        pathMatch: 'full',
      },
      {
        path: 'character',
        loadChildren: () =>
          import('./../../../features/game/character/character.routes').then(
            (m) => m.CHARACTER_ROUTES,
          ),
      },
      {
        path: 'professions',
        loadChildren: () =>
          import(
            './../../../features/game/professions/professions.routes'
          ).then((m) => m.PROFESSIONS_ROUTES),
      },
    ],
  },
];
