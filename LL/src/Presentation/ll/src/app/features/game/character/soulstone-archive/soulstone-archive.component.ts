import { Component, OnInit } from '@angular/core';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { NgFor, NgIf } from '@angular/common';
import { SoulstoneUpgradeCardComponent } from './soulstone-upgrade-card/soulstone-upgrade-card.component';
import { RegularButtonComponent } from '../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { SoulstoneUpgradeStateService } from '../../../../core/services/api/soulstone-upgrade/soulstone-upgrade.state.service';

@Component({
  selector: 'app-soulstone-archive',
  standalone: true,
  imports: [
    DefaultHeaderComponent,
    NgIf,
    NgFor,
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

  error(): string | null {
    return this.soulstoneState.error();
  }

  lastRefund(): number {
    return this.soulstoneState.lastRefund();
  }

  allUpgrades() {
    return this.soulstoneState.upgrades();
  }

  totalUpgradeLevels(): number {
    return this.allUpgrades().reduce(
      (total, upgrade) => total + upgrade.level,
      0,
    );
  }

  maxUpgradeLevels(): number {
    return this.allUpgrades().reduce(
      (total, upgrade) => total + upgrade.definition.maxLevel,
      0,
    );
  }

  maxedUpgradeCount(): number {
    return this.allUpgrades().filter(
      (upgrade) => upgrade.level >= upgrade.definition.maxLevel,
    ).length;
  }

  affordableUpgradeCount(): number {
    const soulstones = this.character()?.soulstones ?? 0;
    return this.allUpgrades().filter(
      (upgrade) => upgrade.nextCost != null && upgrade.nextCost <= soulstones,
    ).length;
  }

  summaryCards(): { label: string; value: string | number }[] {
    const character = this.character();
    return [
      { label: 'Available soulstones', value: character?.soulstones ?? 0 },
      {
        label: 'Levels',
        value: `${this.totalUpgradeLevels()} / ${this.maxUpgradeLevels()}`,
      },
      { label: 'Affordable', value: this.affordableUpgradeCount() },
      { label: 'Maxed', value: this.maxedUpgradeCount() },
    ];
  }
}
