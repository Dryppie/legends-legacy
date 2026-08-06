import { Routes } from '@angular/router';
import { PropheciesPageComponent } from './prophecies-page.component';
import { GUIDE_PAGE_IDS } from '../../../shared/help/guide-catalog';

export const PROPHECIES_ROUTES: Routes = [
  {
    path: '',
    component: PropheciesPageComponent,
    data: { guidePageId: GUIDE_PAGE_IDS.prophecies },
  },
];
