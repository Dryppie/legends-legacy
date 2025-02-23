import { Routes } from '@angular/router';
import { TempleComponent } from './temple/temple.component';
import { CityComponent } from './city.component';
import { TavernComponent } from './tavern/tavern.component';

export const CITY_ROUTES: Routes = [
  {
    path: '',
    component: CityComponent,
    children: [
      {
        path: '',
        redirectTo: 'temple',
        pathMatch: 'full',
      },
      {
        path: 'temple',
        component: TempleComponent,
      },
      {
        path: 'tavern',
        component: TavernComponent,
      },
    ],
  },
];
