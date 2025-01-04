import { Routes } from '@angular/router';
import { TempleComponent } from '../../../shared/components/city/temple/temple.component';
import { CityComponent } from './city.component';

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
    ],
  },
];
