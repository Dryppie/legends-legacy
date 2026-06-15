import { Component, effect, OnInit } from '@angular/core';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { TabComponent } from '../../../../shared/components/custom-components/tabs/tab/tab.component';
import { NgIf } from '@angular/common';
import { CombatComponent } from '../../../../shared/components/combat/combat.component';
import { BattleType } from '../../../../core/state/combat-state/combatState';
import { CombatStateService } from '../../../../core/state/combat-state/combat-state.service';
import { ArenaBattleComponent } from './arena-battle/arena-battle.component';
import { ChampionsMarketComponent } from './champions-market/champions-market.component';
import { RankingsGloryComponent } from './rankings-glory/rankings-glory.component';
import { RecordOfBattleComponent } from './record-of-battle/record-of-battle.component';
import { TournamentGroundsComponent } from './tournament-grounds/tournament-grounds.component';
import { EventBusService } from '../../../../core/services/client-side/event-bus/event-bus.service';
import { ColosseumResultComponent } from '../../../../shared/components/colosseum/colosseum-result/colosseum-result.component';
import { TabsComponent } from '../../../../shared/components/custom-components/tabs/tabs.component';
import { ColosseumStateService } from '../../../../core/services/api/colosseum/colosseum-state.service';

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
  colosseumBattleResult: 'Victory' | 'Defeat' | 'Draw' | null = null;

  battleType = BattleType.Colosseum;

  constructor(
    public combatStateService: CombatStateService,
    public readonly state: ColosseumStateService,
    private eventBus: EventBusService,
  ) {
    effect(
      () => {
        if (this.state.notificationCount() <= 0) return;

        this.state.markNotificationsSeen();
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const finished = this.eventBus.on('colosseum-combat-finished')();
        if (finished) {
          this.displayResultScreen(finished.outcome);
          this.eventBus.clear('colosseum-combat-finished');
          this.state.loadArenaOpponents();
          this.state.loadColosseumMatchResults();
        }
      },
      { allowSignalWrites: true },
    );
  }

  ngOnInit(): void {
    this.state.markNotificationsSeen();
    this.state.refresh();
  }

  displayResultScreen(outcome: 'Victory' | 'Defeat' | 'Draw' | null) {
    this.colosseumBattleResult = outcome;
  }

  hideBattleResult() {
    this.colosseumBattleResult = null;
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
}
