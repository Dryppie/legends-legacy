import { Component, Input } from '@angular/core';
import { SoulstoneUpgradeView } from '../../../../../shared/models/soulstones/soulstone-upgrade-view';
import { CommonModule } from '@angular/common';
import { CharacterDto } from '../../../../../shared/models/Dtos/characterDto';
import { SoulstoneUpgradeStateService } from '../../../../../core/services/api/soulstone-upgrade/soulstone-upgrade.state.service';
import { RegularButtonComponent } from '../../../../../shared/components/buttons/regular-button/regular-button.component';

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
      upgrade.nextCost == null || upgrade.nextCost > this.character?.soulstones
    );
  }

  loading(): boolean {
    return this.soulstoneState.loading();
  }
}
