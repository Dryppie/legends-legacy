import { Component, effect, OnInit } from '@angular/core';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { TabComponent } from '../../../../shared/components/tabs/tab/tab.component';
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
import { ArenaTicketStatus } from '../../../../shared/models/Dtos/colosseum/arenaTicketStatus';
import { TabsComponent } from '../../../../shared/components/tabs/tabs.component';
import { ArenaOpponentPreview } from '../../../../shared/models/Dtos/colosseum/arenaOpponentPreview';
import { EventBusService } from '../../../../core/services/client-side/event-bus/event-bus.service';
import { ColosseumResultComponent } from '../../../../shared/components/colosseum/colosseum-result/colosseum-result.component';
import { LeaderboardEntry } from '../../../../shared/models/Dtos/leaderboard/leaderboardEntry';

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
    ColosseumResultComponent,
  ],
  templateUrl: './colosseum.component.html',
})
export class ColosseumComponent implements OnInit {
  allOpponents: ArenaOpponentPreview[] = [];
  opponents: ArenaOpponentPreview[] = [];
  arenaTicketStatus!: ArenaTicketStatus;

  rankings: LeaderboardEntry[] = [];
  previousMatches: ColosseumMatchResult[] = [];

  colosseumBattleResult: 'Victory' | 'Defeat' | 'Draw' | null = null;

  battleType = BattleType.Colosseum;
  displayCombat = false;
  constructor(
    public combatStateService: CombatStateService,
    private colosseumService: ColosseumService,
    private eventBus: EventBusService,
  ) {
    effect(
      () => {
        const finished = this.eventBus.on('colosseum-combat-finished')();
        if (finished) {
          this.displayResultScreen(finished.outcome); // Your custom logic here
          this.eventBus.clear('colosseum-combat-finished');
          this.loadArenaOpponents();
          this.loadColosseumMatchResults();
        }
      },
      { allowSignalWrites: true },
    );
  }

  displayResultScreen(outcome: 'Victory' | 'Defeat' | 'Draw' | null) {
    this.colosseumBattleResult = outcome;
  }

  hideBattleResult() {
    this.colosseumBattleResult = null;
  }

  ngOnInit(): void {
    this.loadArenaOpponents();
    this.loadColosseumMatchResults();
    this.loadColosseumRankings();

    this.colosseumService.getArenaTicketStatus();
  }

  private loadArenaOpponents(): void {
    this.colosseumService.getArenaOpponents().subscribe({
      next: (data) => {
        this.allOpponents = data;
        this.pickRandomOpponents();
      },
      error: (err) => {
        console.error('Failed to fetch arena opponents:', err);
      },
    });
  }

  private loadColosseumMatchResults(): void {
    this.colosseumService.getColosseumMatchResults().subscribe({
      next: (data) => {
        this.previousMatches = data.sort(
          (a, b) =>
            new Date(b.playedAt).getTime() - new Date(a.playedAt).getTime(),
        );
      },
      error: (err) => {
        console.error('Failed to fetch colosseum match results:', err);
      },
    });
  }

  private loadColosseumRankings(): void {
    this.colosseumService.getColosseumRankings().subscribe({
      next: (data) => {
        this.rankings = data.sort((a, b) => a.rank - b.rank);
      },
      error: (err) => {
        console.error('Failed to fetch colosseum rankings:', err);
      },
    });
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
      .sort((a, b) => b.opponentRating - a.opponentRating);
  }

  onRefreshOpponents(): void {
    this.pickRandomOpponents();
  }

  challenge(id: string) {
    this.colosseumService.startArenaBattle(id);
    this.displayCombat = true;
  }
}
