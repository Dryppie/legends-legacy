import { Component, OnInit } from '@angular/core';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { TabComponent } from '../../../../shared/components/tabs/tab/tab.component';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { ColosseumService } from '../../../../core/services/api/colosseum/colosseum.service';
import { NgIf } from '@angular/common';
import { CombatComponent } from '../../../../shared/components/combat/combat.component';
import { BattleType } from '../../../../core/state/combat-state/combatState';
import { CombatStateService } from '../../../../core/state/combat-state/combat-state.service';
import { ArenaBattleComponent } from './arena-battle/arena-battle.component';
import { ChampionsMarketComponent } from './champions-market/champions-market.component';
import { RankingsGloryComponent } from './rankings-glory/rankings-glory.component';
import { RecordOfBattleComponent } from './record-of-battle/record-of-battle.component';
import { TournamentGroundsComponent } from './tournament-grounds/tournament-grounds.component';
import { ColosseumMatchResult } from '../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { ColosseumRank } from '../../../../shared/models/Dtos/colosseum/colosseumRank';
import { ArenaTicketStatus } from '../../../../shared/models/Dtos/colosseum/arenaTicketStatus';
import { TabsComponent } from '../../../../shared/components/tabs/tabs.component';

@Component({
  selector: 'app-colosseum',
  standalone: true,
  imports: [
    BannerComponent,
    TabComponent,
    CombatComponent,
    NgIf,
    ArenaBattleComponent,
    ChampionsMarketComponent,
    RankingsGloryComponent,
    RecordOfBattleComponent,
    TournamentGroundsComponent,
    TabsComponent,
  ],
  templateUrl: './colosseum.component.html',
})
export class ColosseumComponent implements OnInit {
  allOpponents: CharacterDto[] = [];
  opponents: CharacterDto[] = [];
  arenaTicketStatus!: ArenaTicketStatus;

  rankings: ColosseumRank[] = [];

  previousMatches: ColosseumMatchResult[] = [];

  battleType = BattleType.Colosseum;
  displayCombat = false;
  constructor(
    public combatStateService: CombatStateService,
    private colosseumService: ColosseumService,
  ) {}

  ngOnInit(): void {
    this.colosseumService.getArenaOpponents().subscribe({
      next: (data) => {
        this.allOpponents = data;
        this.pickRandomOpponents();
      },
      error: (err) => {
        console.error('Failed to fetch arena opponents:', err);
      },
    });
    this.colosseumService.getColosseumMatchResults().subscribe({
      next: (data) => {
        this.previousMatches = data.sort(
          (a, b) =>
            new Date(b.playedAt).getTime() - new Date(a.playedAt).getTime(),
        );
      },
    });

    this.colosseumService.getColosseumRankings().subscribe({
      next: (data) => {
        this.rankings = data.sort((a, b) => a.rank - b.rank);
      },
    });

    this.colosseumService.getArenaTicketStatus();
  }

  skipBattle() {
    this.colosseumService.skipColosseumMatch();
  }

  pickRandomOpponents(): void {
    // Shuffle a shallow copy and take the first 5
    this.opponents = this.allOpponents
      .map((x) => ({ ...x }))
      .sort(() => Math.random() - 0.5)
      .slice(0, 5)
      .sort((a, b) => b.arenaRating - a.arenaRating);
  }

  onRefreshOpponents(): void {
    this.pickRandomOpponents();
  }

  challenge(id: string) {
    this.colosseumService.startArenaBattle(id);
    this.displayCombat = true;
  }
}
