import { Routes } from '@angular/router';
import { LandingComponent } from './landing.component';
import { LandingHeroComponent } from './components/landing-hero/landing-hero.component';
import { SignupComponent } from './signup/signup.component';
import { RoadmapComponent } from './roadmap/roadmap.component';
import { LoginComponent } from './login/login.component';
import { WorldComponent } from './world/world.component';
import { FaqComponent } from './faq/faq.component';

export const LANDING_ROUTES: Routes = [
  {
    path: '',
    component: LandingComponent,
    children: [
      {
        path: '',
        redirectTo: 'login',
        pathMatch: 'full',
      },
      // {
      //   path: '',
      //   component: LandingHeroComponent,
      // },
      // {
      //   path: 'world',
      //   component: WorldComponent,
      // },
      // {
      //   path: 'roadmap',
      //   component: RoadmapComponent,
      // },
      // {
      //   path: 'faq',
      //   component: FaqComponent,
      // },
      {
        path: 'login',
        component: LoginComponent,
      },
      {
        path: 'signup',
        component: SignupComponent,
      },
    ],
  },
];
