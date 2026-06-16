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
  @Input() upgradeType: string = '';

  constructor(private readonly soulstoneState: SoulstoneUpgradeStateService) {}

  upgrade(up: SoulstoneUpgradeView): void {
    this.soulstoneState.upgrade(up.definition.id);
  }

  disablePurchase(upgrade: SoulstoneUpgradeView): boolean {
    return (
      upgrade.nextCost == null ||
      upgrade.nextCost > this.character?.soulstones ||
      this.soulstoneState.isUpgradeLoading(upgrade.definition.id)()
    );
  }

  loading(): boolean {
    return this.soulstoneState.loading();
  }

  currentEffect(upgrade: SoulstoneUpgradeView): string {
    const value = upgrade.level * upgrade.definition.effect.perLevel;
    return `${value > 0 ? '+' : ''}${value.toFixed(1)}%`;
  }

  progressPercent(upgrade: SoulstoneUpgradeView): number {
    if (upgrade.definition.maxLevel <= 0) return 100;
    return Math.min(100, (upgrade.level / upgrade.definition.maxLevel) * 100);
  }

  statusLabel(upgrade: SoulstoneUpgradeView): string {
    if (upgrade.nextCost == null) return 'Maxed';
    if (this.soulstoneState.isUpgradeLoading(upgrade.definition.id)()) {
      return 'Upgrading';
    }
    return upgrade.nextCost <= (this.character?.soulstones ?? 0)
      ? 'Upgradable'
      : 'Need Soulstones';
  }

  statusClass(upgrade: SoulstoneUpgradeView): string {
    if (upgrade.nextCost == null) {
      return 'border-primary/50 bg-primary/10 text-primary';
    }

    return upgrade.nextCost <= (this.character?.soulstones ?? 0)
      ? 'border-emerald-400/40 bg-emerald-500/10 text-emerald-200'
      : 'border-light_gray/50 bg-black/20 text-zinc-400';
  }

  upgradeButtonText(upgrade: SoulstoneUpgradeView): string {
    if (this.soulstoneState.isUpgradeLoading(upgrade.definition.id)()) {
      return 'Upgrading';
    }
    if (upgrade.nextCost == null) return 'Maxed';
    return 'Upgrade';
  }
}
