import { Component, OnInit } from '@angular/core';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { NgIf } from '@angular/common';
import { SoulstoneUpgradeCardComponent } from './soulstone-upgrade-card/soulstone-upgrade-card.component';
import { RegularButtonComponent } from '../../../../shared/components/buttons/regular-button/regular-button.component';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { SoulstoneUpgradeStateService } from '../../../../core/services/api/soulstone-upgrade/soulstone-upgrade.state.service';

@Component({
  selector: 'app-soulstone-archive',
  standalone: true,
  imports: [
    DefaultHeaderComponent,
    NgIf,
    SoulstoneUpgradeCardComponent,
    RegularButtonComponent,
  ],
  templateUrl: './soulstone-archive.component.html',
})
export class SoulstoneArchiveComponent implements OnInit {
  readonly character;

  // Signals exposed from the state service
  readonly combatUpgrades;
  readonly gatheringUpgrades;
  readonly craftingUpgrades;
  readonly miscUpgrades;

  constructor(
    private readonly state: CharacterStateService,
    private readonly soulstoneState: SoulstoneUpgradeStateService,
  ) {
    this.character = this.state.currentCharacter;
    this.combatUpgrades = this.soulstoneState.combatUpgrades;
    this.gatheringUpgrades = this.soulstoneState.gatheringUpgrades;
    this.craftingUpgrades = this.soulstoneState.craftingUpgrades;
    this.miscUpgrades = this.soulstoneState.miscUpgrades;
  }

  ngOnInit(): void {
    this.soulstoneState.load();
  }

  resetSoulstoneUpgrades(): void {
    this.soulstoneState.reset();
  }

  loading(): boolean {
    return this.soulstoneState.loading();
  }
}
