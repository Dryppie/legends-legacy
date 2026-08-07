import { Routes } from '@angular/router';
import { CityComponent } from './city.component';
import { TavernComponent } from './tavern/tavern.component';
import { ColosseumComponent } from './colosseum/colosseum.component';
import { TournamentReplayComponent } from './colosseum/tournament-replay/tournament-replay.component';
import { GuildComponent } from './guild/guild.component';
import { MarketPlaceComponent } from './market-place/market-place.component';
import { GUIDE_PAGE_IDS } from '../../../shared/help/guide-catalog';

export const CITY_ROUTES: Routes = [
  {
    path: '',
    component: CityComponent,
    children: [
      {
        path: '',
        redirectTo: 'guild',
        pathMatch: 'full',
      },
      {
        path: 'guild',
        component: GuildComponent,
        data: { guidePageId: GUIDE_PAGE_IDS.guild },
      },
      {
        path: 'colosseum/tournaments/:tournamentId/matches/:matchId/replay',
        component: TournamentReplayComponent,
        data: { guidePageId: GUIDE_PAGE_IDS.tournamentReplay },
      },
      {
        path: 'colosseum',
        component: ColosseumComponent,
        data: { guidePageId: GUIDE_PAGE_IDS.colosseum },
      },
      {
        path: 'market-place',
        component: MarketPlaceComponent,
        data: { guidePageId: GUIDE_PAGE_IDS.marketplace },
      },
      {
        path: 'tavern',
        component: TavernComponent,
        data: { guideDisabled: true },
      },
    ],
  },
];
