import { Component, effect, OnInit } from '@angular/core';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { TabComponent } from '../../../../shared/components/custom-components/tabs/tab/tab.component';
import { DatePipe, NgIf } from '@angular/common';
import { CombatComponent } from '../../../../shared/components/combat/combat.component';
import { BattleType } from '../../../../core/state/combat-state/combatState';
import { CombatStateService } from '../../../../core/state/combat-state/combat-state.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { ArenaBattleComponent } from './arena-battle/arena-battle.component';
import { ChampionsMarketComponent } from './champions-market/champions-market.component';
import { RankingsGloryComponent } from './rankings-glory/rankings-glory.component';
import { RecordOfBattleComponent } from './record-of-battle/record-of-battle.component';
import { TournamentGroundsComponent } from './tournament-grounds/tournament-grounds.component';
import { EventBusService } from '../../../../core/services/client-side/event-bus/event-bus.service';
import { ColosseumResultComponent } from '../../../../shared/components/colosseum/colosseum-result/colosseum-result.component';
import { TabsComponent } from '../../../../shared/components/custom-components/tabs/tabs.component';
import { ColosseumStateService } from '../../../../core/services/api/colosseum/colosseum-state.service';
import { LeaderboardEntry } from '../../../../shared/models/Dtos/leaderboard/leaderboardEntry';
import { ColosseumMatchResult } from '../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { NumberFormatPipe } from '../../../../shared/pipes/number-format/number-format.pipe';

@Component({
  selector: 'app-colosseum',
  standalone: true,
  imports: [
    BannerComponent,
    TabComponent,
    CombatComponent,
    NgIf,
    DatePipe,
    ArenaBattleComponent,
    ChampionsMarketComponent,
    RankingsGloryComponent,
    RecordOfBattleComponent,
    TournamentGroundsComponent,
    TabsComponent,
    ColosseumResultComponent,
    NumberFormatPipe,
  ],
  templateUrl: './colosseum.component.html',
})
export class ColosseumComponent implements OnInit {
  battleType = BattleType.Colosseum;

  constructor(
    public combatStateService: CombatStateService,
    public readonly state: ColosseumStateService,
    private readonly characterState: CharacterStateService,
    private eventBus: EventBusService,
  ) {
    effect(
      () => {
        const finished = this.eventBus.on('colosseum-combat-finished')();
        if (finished) {
          this.eventBus.clear('colosseum-combat-finished');
          this.state.loadStatus();
          this.state.loadArenaOpponents();
          this.state.loadColosseumMatchResults();
        }
      },
      { allowSignalWrites: true },
    );
  }

  ngOnInit(): void {
    this.state.refresh();
  }

  hideBattleResult() {
    this.state.clearLatestBattleResult();
  }

  skipBattle() {
    this.state.skipColosseumMatch();
  }

  onRefreshOpponents(): void {
    this.state.pickRandomOpponents();
  }

  challenge(id: string) {
    this.state.startArenaBattle(id);
  }

  updateDefenseSnapshot(): void {
    this.state.updateDefenseSnapshot();
  }

  get myRanking(): LeaderboardEntry | undefined {
    const id = this.characterState.currentCharacterId();
    if (!id) return undefined;

    return this.state.rankings().find((ranking) => ranking.characterId === id);
  }

  get recentRecord(): { wins: number; losses: number; draws: number } {
    const id = this.characterState.currentCharacterId();
    if (!id) return { wins: 0, losses: 0, draws: 0 };

    return this.state.previousMatches().reduce(
      (record, match) => {
        if (!this.includesCurrentCharacter(match, id)) return record;

        if (!match.winnerId) {
          record.draws += 1;
        } else if (match.winnerId === id) {
          record.wins += 1;
        } else {
          record.losses += 1;
        }

        return record;
      },
      { wins: 0, losses: 0, draws: 0 },
    );
  }

  private includesCurrentCharacter(
    match: ColosseumMatchResult,
    id: string,
  ): boolean {
    return match.characterAId === id || match.characterBId === id;
  }
}
