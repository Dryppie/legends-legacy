import { Component, OnInit } from '@angular/core';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { TabComponent } from '../../../../shared/components/tab/tab.component';
import { Tab } from '../../../../shared/models/sidebar-item';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { ColosseumService } from '../../../../core/services/api/colosseum/colosseum.service';
import { AsyncPipe, NgFor, NgIf } from '@angular/common';
import { CombatComponent } from '../../../../shared/components/combat/combat.component';
import { BattleType } from '../../../../core/state/combat-state/combatState';
import { CombatStateService } from '../../../../core/state/combat-state/combat-state.service';

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
  ],
  templateUrl: './colosseum.component.html',
  styleUrl: './colosseum.component.css',
})
export class ColosseumComponent implements OnInit {
  opponents!: CharacterDto[];
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
        // Store the fetched data in the component property
        this.opponents = data;
      },
      error: (err) => {
        console.error('Failed to fetch arena opponents:', err);
      },
    });
  }

  challenge(id: string) {
    this.colosseumService.startArenaBattle(id);
    this.displayCombat = true;
  }

  tabs: Tab[] = [
    {
      label: 'Arena Battle',
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
    // {
    //   label: 'Rankings & Glory',
    //   items: [],
    // },
    // {
    //   label: 'Record of Battle',
    //   items: [],
    // },
  ];
  activeTab: string = '';

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }
}
