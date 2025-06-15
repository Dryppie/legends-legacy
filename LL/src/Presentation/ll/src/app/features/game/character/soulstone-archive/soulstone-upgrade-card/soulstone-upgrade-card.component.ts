import { Component, Input } from '@angular/core';
import { SoulstoneUpgradeView } from '../../../../../shared/models/soulstones/soulstone-upgrade-view';
import { CommonModule } from '@angular/common';
import { SoulstoneUpgradeService } from '../../../../../core/services/api/soulstone-upgrade/soulstone-upgrade.service';
import { filter } from 'rxjs';
import { CharacterDto } from '../../../../../shared/models/Dtos/characterDto';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';

@Component({
  selector: 'app-soulstone-upgrade-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './soulstone-upgrade-card.component.html',
})
export class SoulstoneUpgradeCardComponent {
  @Input() character!: CharacterDto;
  @Input() upgrades: SoulstoneUpgradeView[] = [];
  @Input() upgradeType: string = '';

  constructor(
    private readonly state: CharacterStateService,
    private readonly soulstoneUpgradeService: SoulstoneUpgradeService,
  ) {}

  upgrade(up: SoulstoneUpgradeView): void {
    const cost = up.nextCost;
    if (cost == null || this.disablePurchase(up)) return;

    this.soulstoneUpgradeService
      .upgrade(up.definition.id) // returns Observable<boolean>
      .pipe(filter(Boolean)) // success only
      .subscribe({
        next: () => {
          /* 1️⃣  update the local view model --------------------------------- */
          up.level += 1;

          const { increment, incrementCap } = up.definition.cost;
          const nextLvl = up.level + 1;
          const maxLvl = up.definition.maxLevel;

          let next = nextLvl <= maxLvl ? cost + increment : undefined;
          if (incrementCap && next && next > incrementCap) next = incrementCap;
          up.nextCost = next;

          /* 2️⃣  update the character in global state ----------------------- */
          const current = this.state.currentCharacter(); // ← signal read
          if (current) {
            this.state.updateCharacter({
              ...current,
              soulstones: current.soulstones - cost,
            });
          }
        },
        error: (err) => console.error('Upgrade failed:', err),
      });
  }

  disablePurchase(upgrade: SoulstoneUpgradeView): boolean {
    return (
      upgrade.nextCost == null || upgrade.nextCost > this.character?.soulstones
    );
  }
}
