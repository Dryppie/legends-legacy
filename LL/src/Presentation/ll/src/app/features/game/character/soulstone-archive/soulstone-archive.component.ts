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
  readonly branchGroups;
  resetConfirmationOpen = false;

  constructor(
    private readonly state: CharacterStateService,
    private readonly soulstoneState: SoulstoneUpgradeStateService,
  ) {
    this.character = this.state.currentCharacter;
    this.branchGroups = this.soulstoneState.branchGroups;
  }

  ngOnInit(): void {
    this.soulstoneState.load();
  }

  openResetConfirmation(): void {
    if (this.loading() || this.totalUpgradeRanks() === 0) return;

    this.resetConfirmationOpen = true;
  }

  cancelReset(): void {
    this.resetConfirmationOpen = false;
  }

  confirmResetSoulstoneUpgrades(): void {
    this.resetConfirmationOpen = false;
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

  resetRefund(): number {
    return this.soulstoneState.resetRefund();
  }

  allUpgrades() {
    return this.soulstoneState.upgrades();
  }

  totalUpgradeRanks(): number {
    return this.allUpgrades().reduce(
      (total, upgrade) => total + upgrade.currentRank,
      0,
    );
  }

  maxUpgradeRanks(): number {
    return this.allUpgrades().reduce(
      (total, upgrade) => total + upgrade.maxRank,
      0,
    );
  }

  maxedUpgradeCount(): number {
    return this.allUpgrades().filter(
      (upgrade) => upgrade.currentRank >= upgrade.maxRank,
    ).length;
  }

  affordableUpgradeCount(): number {
    return this.allUpgrades().filter((upgrade) => upgrade.canPurchase).length;
  }

  summaryCards(): { label: string; value: string | number }[] {
    const character = this.character();
    return [
      { label: 'Available soulstones', value: character?.soulstones ?? 0 },
      {
        label: 'Ranks',
        value: `${this.totalUpgradeRanks()} / ${this.maxUpgradeRanks()}`,
      },
      { label: 'Purchasable', value: this.affordableUpgradeCount() },
      { label: 'Maxed', value: this.maxedUpgradeCount() },
    ];
  }
}
