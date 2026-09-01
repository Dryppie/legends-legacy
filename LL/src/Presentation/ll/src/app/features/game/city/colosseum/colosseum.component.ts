import {
  Component,
  effect,
  HostListener,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { TabComponent } from '../../../../shared/components/custom-components/tabs/tab/tab.component';
import { Location, NgClass, NgIf, NgTemplateOutlet } from '@angular/common';
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
import { TabsComponent } from '../../../../shared/components/custom-components/tabs/tabs.component';
import { ColosseumStateService } from '../../../../core/services/api/colosseum/colosseum-state.service';
import { LeaderboardEntry } from '../../../../shared/models/Dtos/leaderboard/leaderboardEntry';
import { ColosseumMatchResult } from '../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { NumberFormatPipe } from '../../../../shared/pipes/number-format/number-format.pipe';
import { LocalDatePipe } from '../../../../shared/pipes/local-date/local-date.pipe';
import { CombatEntityStatsComponent } from '../../../../shared/components/combat/combat-entity-stats/combat-entity-stats.component';
import { MiniButtonComponent } from '../../../../shared/components/custom-components/buttons/mini-button/mini-button.component';

@Component({
  selector: 'app-colosseum',
  imports: [
    DefaultHeaderComponent,
    TabComponent,
    CombatComponent,
    NgClass,
    NgIf,
    NgTemplateOutlet,
    LocalDatePipe,
    ArenaBattleComponent,
    ChampionsMarketComponent,
    RankingsGloryComponent,
    RecordOfBattleComponent,
    TournamentGroundsComponent,
    TabsComponent,
    NumberFormatPipe,
    CombatEntityStatsComponent,
    MiniButtonComponent,
  ],
  templateUrl: './colosseum.component.html',
})
export class ColosseumComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);

  battleType = BattleType.Colosseum;
  readonly selectedCombatSummary = signal<ColosseumMatchResult | null>(null);
  readonly selectedTabIndex = toSignal(
    this.route.queryParamMap.pipe(
      map((params) => colosseumTabIndex(params.get('tab'))),
    ),
    { initialValue: 0 },
  );

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
    const navigationState = this.location.getState() as {
      preserveColosseum?: boolean;
    };
    if (navigationState.preserveColosseum !== true) {
      this.state.refresh();
    }
  }

  skipBattle() {
    this.state.skipColosseumMatch();
  }

  openCombatSummary(match: ColosseumMatchResult): void {
    if (match.combatSummary) this.selectedCombatSummary.set(match);
  }

  closeCombatSummary(): void {
    this.selectedCombatSummary.set(null);
  }

  @HostListener('document:keydown.escape')
  closeCombatSummaryOnEscape(): void {
    if (this.selectedCombatSummary()) this.closeCombatSummary();
  }

  combatDurationLabel(durationTicks: number): string {
    const totalSeconds = Math.max(0, Math.round(durationTicks / 10));
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return minutes > 0 ? `${minutes}m ${seconds}s` : `${seconds}s`;
  }

  combatSummaryResultLabel(match: ColosseumMatchResult): string {
    const id = this.characterState.currentCharacterId();
    if (!match.winnerId) return 'Draw';
    return match.winnerId === id ? 'Victory' : 'Defeat';
  }

  combatSummaryResultClass(match: ColosseumMatchResult): string {
    const result = this.combatSummaryResultLabel(match);
    if (result === 'Victory') return 'll-badge-success';
    if (result === 'Defeat') return 'll-badge-danger';
    return 'll-badge-warning';
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

  selectTab(index: number): void {
    const tab = COLOSSEUM_TABS[index] ?? COLOSSEUM_TABS[0];
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab: tab === 'arena' ? null : tab },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  get myRanking(): LeaderboardEntry | undefined {
    const id = this.characterState.currentCharacterId();
    if (!id) return undefined;

    return this.state.rankings().find((ranking) => ranking.characterId === id);
  }

  get currentCharacterId(): string | null {
    return this.characterState.currentCharacterId();
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

const COLOSSEUM_TABS = [
  'arena',
  'tournaments',
  'market',
  'rankings',
  'record',
] as const;

export function colosseumTabIndex(tab: string | null): number {
  const index = COLOSSEUM_TABS.findIndex(
    (candidate) => candidate === tab?.toLowerCase(),
  );
  return index >= 0 ? index : 0;
}
