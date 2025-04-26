import { Component, OnInit } from '@angular/core';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { TabComponent } from '../../../../shared/components/tab/tab.component';
import { Tab } from '../../../../shared/models/sidebar-item';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { ColosseumService } from '../../../../core/services/api/colosseum/colosseum.service';
import {
  AsyncPipe,
  NgFor,
  NgIf,
  NgSwitch,
  NgSwitchCase,
} from '@angular/common';
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

@Component({
  selector: 'app-colosseum',
  standalone: true,
  imports: [
    BannerComponent,
    TabComponent,
    NgFor,
    CombatComponent,
    NgIf,
    AsyncPipe,
    NgSwitch,
    NgSwitchCase,
    ArenaBattleComponent,
    ChampionsMarketComponent,
    RankingsGloryComponent,
    RecordOfBattleComponent,
    TournamentGroundsComponent,
  ],
  templateUrl: './colosseum.component.html',
  styleUrl: './colosseum.component.css',
})
export class ColosseumComponent implements OnInit {
  allOpponents: CharacterDto[] = [];
  opponents: CharacterDto[] = [];

  rankings: ColosseumRank[] = [];

  previousMatches: ColosseumMatchResult[] = [];

  battleType = BattleType.Colosseum;
  displayCombat = false;
  constructor(
    public combatStateService: CombatStateService,
    private colosseumService: ColosseumService,
  ) {}

  ngOnInit(): void {
    this.setActiveTab(this.tabs[0]?.label || '');
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

  tabs: Tab[] = [
    {
      label: 'Arena',
      items: [],
    },
    // {
    //   label: 'Tournament Grounds',
    //   items: [],
    // },
    // {
    //   label: `Champion's Market`,
    //   items: [],
    // },
    {
      label: 'Rankings & Glory',
      items: [],
    },
    {
      label: 'Record of Battles',
      items: [],
    },
  ];
  activeTab: string = '';

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }
}
