import { Routes } from '@angular/router';
import { SettingsComponent } from './settings.component';
import { GUIDE_PAGE_IDS } from '../../../shared/help/guide-catalog';

export const SETTINGS_ROUTES: Routes = [
  {
    path: '',
    component: SettingsComponent,
    data: { guidePageId: GUIDE_PAGE_IDS.settings },
  },
];
