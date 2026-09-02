import { Routes } from '@angular/router';
import { CityComponent } from './city.component';
import { TavernComponent } from './tavern/tavern.component';
import { ColosseumComponent } from './colosseum/colosseum.component';
import { TournamentReplayComponent } from './colosseum/tournament-replay/tournament-replay.component';
import { GuildComponent } from './guild/guild.component';
import { MarketPlaceComponent } from './market-place/market-place.component';
import { GUIDE_PAGE_IDS } from '../../../shared/help/guide-catalog';
import { focusedBetaMinimumLevelGuard } from '../../../core/guards/focused-beta-journey.guard';
import {
  PLAYER_JOURNEY_ECONOMY_UNLOCK_LEVEL,
  PLAYER_JOURNEY_SOCIAL_UNLOCK_LEVEL,
} from '../../../core/services/client-side/player-journey/player-journey';

const focusedBetaSocialGuard = focusedBetaMinimumLevelGuard(
  PLAYER_JOURNEY_SOCIAL_UNLOCK_LEVEL,
);
const focusedBetaEconomyGuard = focusedBetaMinimumLevelGuard(
  PLAYER_JOURNEY_ECONOMY_UNLOCK_LEVEL,
);

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
        canActivate: [focusedBetaSocialGuard],
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
        canActivate: [focusedBetaEconomyGuard],
        data: { guidePageId: GUIDE_PAGE_IDS.marketplace },
      },
      {
        path: 'tavern',
        component: TavernComponent,
        canActivate: [focusedBetaEconomyGuard],
        data: { guideDisabled: true },
      },
    ],
  },
];
