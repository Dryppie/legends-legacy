import { Component, Input } from '@angular/core';
import { SoulstoneUpgradeView } from '../../../../../shared/models/soulstones/soulstone-upgrade-view';
import { CommonModule } from '@angular/common';
import { CharacterDto } from '../../../../../shared/models/Dtos/characterDto';
import { SoulstoneUpgradeStateService } from '../../../../../core/services/api/soulstone-upgrade/soulstone-upgrade.state.service';
import { RegularButtonComponent } from '../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';

@Component({
  selector: 'app-soulstone-upgrade-card',
  standalone: true,
  imports: [CommonModule, RegularButtonComponent],
  templateUrl: './soulstone-upgrade-card.component.html',
})
export class SoulstoneUpgradeCardComponent {
  @Input() character!: CharacterDto;
  @Input() upgrades: SoulstoneUpgradeView[] = [];
  @Input() title = '';

  constructor(private readonly soulstoneState: SoulstoneUpgradeStateService) {}

  upgrade(upgrade: SoulstoneUpgradeView): void {
    this.soulstoneState.upgrade(upgrade.id);
  }

  disablePurchase(upgrade: SoulstoneUpgradeView): boolean {
    return (
      !upgrade.canPurchase ||
      this.soulstoneState.isUpgradeLoading(upgrade.id)()
    );
  }

  loading(): boolean {
    return this.soulstoneState.loading();
  }

  progressPercent(upgrade: SoulstoneUpgradeView): number {
    if (upgrade.maxRank <= 0) return 100;
    return Math.min(100, (upgrade.currentRank / upgrade.maxRank) * 100);
  }

  statusLabel(upgrade: SoulstoneUpgradeView): string {
    if (this.soulstoneState.isUpgradeLoading(upgrade.id)()) {
      return 'Upgrading';
    }
    if (upgrade.currentRank >= upgrade.maxRank) return 'Maxed';
    return upgrade.canPurchase ? 'Available' : 'Locked';
  }

  statusClass(upgrade: SoulstoneUpgradeView): string {
    if (upgrade.currentRank >= upgrade.maxRank) {
      return 'll-badge-accent';
    }

    return upgrade.canPurchase ? 'll-badge-success' : 'll-badge-muted';
  }

  upgradeButtonText(upgrade: SoulstoneUpgradeView): string {
    if (this.soulstoneState.isUpgradeLoading(upgrade.id)()) {
      return 'Upgrading';
    }
    if (upgrade.currentRank >= upgrade.maxRank) return 'Maxed';
    return upgrade.nextCost == null ? 'Locked' : `Upgrade`;
  }
}
