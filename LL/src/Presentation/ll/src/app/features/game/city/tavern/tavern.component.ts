import { Component } from '@angular/core';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { LeaderboardStateService } from '../../../../core/services/api/leaderboard/leaderboard-state.service';
import { FilterTabsComponent } from '../../../../shared/components/custom-components/tabs/filter-tabs/filter-tabs.component';
import {
  LeaderboardColumn,
  LeaderboardEntry,
} from '../../../../shared/models/Dtos/leaderboard/leaderboardEntry';
import { SidebarSection, Tab } from '../../../../shared/models/sidebar-item';
import { RegularButtonComponent } from '../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { LeaderboardComponent } from '../../../../shared/components/generic-leaderboard/generic-leaderboard.component';
import { COLUMNS_BY_TAB } from '../../../../shared/models/Dtos/leaderboard/leaderboard.config';

@Component({
  selector: 'app-tavern',
  standalone: true,
  imports: [
    BannerComponent,
    FilterTabsComponent,
    RegularButtonComponent,
    LeaderboardComponent,
  ],
  templateUrl: './tavern.component.html',
})
export class TavernComponent {
  constructor(public state: LeaderboardStateService) {}

  tabs: SidebarSection[] = [
    {
      id: 'total-level',
      label: 'Total level',
      items: [],
    },
    {
      id: 'combat',
      label: 'Combat',
      items: [],
    },
    {
      id: 'wealth',
      label: 'Wealth',
      items: [],
    },
    {
      id: 'mining',
      label: 'Mining',
      items: [],
    },
    {
      id: 'woodcutting',
      label: 'Woodcutting',
      items: [],
    },
    {
      id: 'crafting',
      label: 'Crafting',
      items: [],
    },
  ];
  activeTab: string = '';

  ngOnInit(): void {
    this.state.load();
    this.setActiveTab(this.tabs[0]?.label || '');
  }

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }

  refresh(): void {
    this.state.refresh();
  }

  get rows(): LeaderboardEntry[] {
    switch (this.activeTab) {
      case 'Total level':
        return this.state.topTotal();
      case 'Combat':
        return this.state.topCombat();
      case 'Wealth':
        return this.state.topWealth();
      case 'Mining':
        return this.state.byProfession('Mining')();
      case 'Woodcutting':
        return this.state.byProfession('Woodcutting')();
      case 'Crafting':
        return this.state.byProfession('Crafting')();
      default:
        return [];
    }
  }

  get columns(): readonly LeaderboardColumn<LeaderboardEntry>[] {
    // The cast is only to widen the row-specific column set to the generic
    // `LeaderboardColumn<LeaderboardEntry>[]` that <app-leaderboard> expects.
    return COLUMNS_BY_TAB[
      this.activeTab as keyof typeof COLUMNS_BY_TAB
    ] as readonly LeaderboardColumn<LeaderboardEntry>[];
  }

  get highlightId(): string | number | undefined {
    return 'this.state.currentPlayerId?.()';
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }

  get loading() {
    return this.state.loading();
  }
}
