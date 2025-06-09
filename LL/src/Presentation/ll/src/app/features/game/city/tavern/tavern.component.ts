import { Component } from '@angular/core';
import { NgFor } from '@angular/common';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { LeaderboardStateService } from '../../../../core/services/api/leaderboard/leaderboard-state.service';
import { FilterTabsComponent } from '../../../../shared/components/tabs/filter-tabs/filter-tabs.component';
import { LeaderboardEntryDto } from '../../../../shared/models/Dtos/leaderboard/leaderboardEntryDto';
import { Tab } from '../../../../shared/models/sidebar-item';
import { RegularButtonComponent } from '../../../../shared/components/buttons/regular-button/regular-button.component';

@Component({
  selector: 'app-tavern',
  standalone: true,
  imports: [
    BannerComponent,
    NgFor,
    FilterTabsComponent,
    RegularButtonComponent,
  ],
  templateUrl: './tavern.component.html',
})
export class TavernComponent {
  constructor(public state: LeaderboardStateService) {}

  tabs: Tab[] = [
    {
      label: 'Combat',
      items: [],
    },
    {
      label: 'Mining',
      items: [],
    },
    {
      label: 'Woodcutting',
      items: [],
    },
    {
      label: 'Armorforging',
      items: [],
    },
    {
      label: 'Jewelrycrafting',
      items: [],
    },
    {
      label: 'Weaponsmithing',
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

  get filteredLeaderboard(): LeaderboardEntryDto[] {
    switch (this.activeTab) {
      case 'Combat':
        return this.state.topCombat();

      case 'Mining':
        return this.state.byProfession('Mining')();

      case 'Woodcutting':
        return this.state.byProfession('Woodcutting')();

      case 'Armorforging':
        return this.state.byProfession('ArmorForging')();

      case 'Jewelrycrafting':
        return this.state.byProfession('JewelryCrafting')();

      case 'Weaponsmithing':
        return this.state.byProfession('WeaponSmithing')();

      default:
        return this.state.topCombat();
    }
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }

  get loading() {
    return this.state.loading();
  }
}
