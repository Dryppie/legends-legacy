import { Component, Input } from '@angular/core';
import { SoulstoneUpgradeView } from '../../../../../shared/models/soulstones/soulstone-upgrade-view';
import { CommonModule } from '@angular/common';
import { CharacterService } from '../../../../../core/services/api/character/character.service';
import { SoulstoneUpgradeService } from '../../../../../core/services/api/soulstone-upgrade/soulstone-upgrade.service';
import { filter, switchMap, take, tap } from 'rxjs';
import { CharacterDto } from '../../../../../shared/models/Dtos/characterDto';

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
    private readonly characterService: CharacterService,
    private readonly soulstoneUpgradeService: SoulstoneUpgradeService,
  ) {}

  upgrade(upgrade: SoulstoneUpgradeView): void {
    const cost = upgrade.nextCost;
    if (cost == null || this.disablePurchase(upgrade)) return;

    this.soulstoneUpgradeService
      .upgrade(upgrade.definition.id)
      .pipe(
        filter((success) => success),
        tap(() => {
          // Update the upgrade object locally
          upgrade.level += 1;

          const increment = upgrade.definition.cost.increment;
          const incrementCap = upgrade.definition.cost.incrementCap;
          const nextLevel = upgrade.level + 1;
          const maxLevel = upgrade.definition.maxLevel;

          upgrade.nextCost =
            nextLevel <= maxLevel ? cost + increment : undefined;
          if (
            incrementCap &&
            upgrade.nextCost &&
            incrementCap < upgrade.nextCost
          )
            upgrade.nextCost = incrementCap;
        }),
        switchMap(() =>
          this.characterService.getCurrentCharacter().pipe(take(1)),
        ),
        tap((character) => {
          if (character) {
            const updatedCharacter = {
              ...character,
              soulstones: character.soulstones - cost,
            };
            this.characterService.updateCharacter(updatedCharacter);
          }
        }),
      )
      .subscribe({
        error: (err) => console.error('Upgrade failed:', err),
      });
  }

  disablePurchase(upgrade: SoulstoneUpgradeView): boolean {
    return (
      upgrade.nextCost == null || upgrade.nextCost > this.character?.soulstones
    );
  }
}
