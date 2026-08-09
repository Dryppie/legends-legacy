import { Routes } from '@angular/router';
import { QuestJournalPageComponent } from './quest-journal-page.component';

export const QUESTS_ROUTES: Routes = [
  {
    path: '',
    component: QuestJournalPageComponent,
    data: { guideDisabled: true },
  },
];
