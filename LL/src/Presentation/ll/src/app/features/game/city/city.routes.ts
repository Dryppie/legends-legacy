import { Routes } from '@angular/router';
import { CityComponent } from './city.component';
import { TavernComponent } from './tavern/tavern.component';
import { ColosseumComponent } from './colosseum/colosseum.component';
import { TournamentReplayComponent } from './colosseum/tournament-replay/tournament-replay.component';
import { GuildComponent } from './guild/guild.component';
import { MarketPlaceComponent } from './market-place/market-place.component';

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
      },
      {
        path: 'colosseum/tournaments/:tournamentId/matches/:matchId/replay',
        component: TournamentReplayComponent,
      },
      {
        path: 'colosseum',
        component: ColosseumComponent,
      },
      {
        path: 'market-place',
        component: MarketPlaceComponent,
      },
      {
        path: 'tavern',
        component: TavernComponent,
      },
    ],
  },
];
