import { Component, OnInit } from '@angular/core';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { AsyncPipe, NgIf } from '@angular/common';
import { SoulstoneUpgradeService } from '../../../../core/services/api/soulstone-upgrade/soulstone-upgrade.service';
import { forkJoin, map, Observable, shareReplay, switchMap, take } from 'rxjs';
import { SoulstoneUpgradeView } from '../../../../shared/models/soulstones/soulstone-upgrade-view';
import { SoulstoneUpgradeType } from '../../../../shared/models/soulstones/soulstone-upgrade-type';
import { SoulstoneUpgradeCardComponent } from './soulstone-upgrade-card/soulstone-upgrade-card.component';
import { RegularButtonComponent } from '../../../../shared/components/buttons/regular-button/regular-button.component';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { CostCurve } from '../../../../shared/models/soulstones/cost-curve';

@Component({
  selector: 'app-soulstone-archive',
  standalone: true,
  imports: [
    DefaultHeaderComponent,
    AsyncPipe,
    NgIf,
    SoulstoneUpgradeCardComponent,
    RegularButtonComponent,
  ],
  templateUrl: './soulstone-archive.component.html',
})
export class SoulstoneArchiveComponent implements OnInit {
  readonly character;
  readonly soulstoneUpgrades$;

  grouped$!: Observable<SoulstoneUpgradeView[]>;
  combatUpgrades$!: Observable<SoulstoneUpgradeView[]>;
  gatheringUpgrades$!: Observable<SoulstoneUpgradeView[]>;
  craftingUpgrades$!: Observable<SoulstoneUpgradeView[]>;
  miscUpgrades$!: Observable<SoulstoneUpgradeView[]>;

  constructor(
    private readonly state: CharacterStateService,
    private readonly soulstoneUpgradeService: SoulstoneUpgradeService,
  ) {
    this.character = this.state.currentCharacter;
    this.soulstoneUpgrades$ = this.soulstoneUpgradeService.soulstoneUpgrades$;
  }

  ngOnInit(): void {
    this.grouped$ = this.soulstoneUpgrades$.pipe(shareReplay(1));

    this.combatUpgrades$ = this.grouped$.pipe(
      map((list) =>
        list.filter((u) => u.definition.type === SoulstoneUpgradeType.Combat),
      ),
    );
    this.gatheringUpgrades$ = this.grouped$.pipe(
      map((list) =>
        list.filter(
          (u) => u.definition.type === SoulstoneUpgradeType.Gathering,
        ),
      ),
    );
    this.craftingUpgrades$ = this.grouped$.pipe(
      map((list) =>
        list.filter((u) => u.definition.type === SoulstoneUpgradeType.Crafting),
      ),
    );
    this.miscUpgrades$ = this.grouped$.pipe(
      map((list) =>
        list.filter((u) => u.definition.type === SoulstoneUpgradeType.Misc),
      ),
    );
  }

  resetSoulstoneUpgrades(): void {
    const character = this.state.currentCharacter();
    if (!character) return;

    // Step 1: read current upgrades from observable
    this.grouped$.pipe(take(1)).subscribe((upgrades) => {
      let refund = 0;

      // Step 2: locally compute refund and reset view models
      for (const up of upgrades) {
        const def = up.definition;

        for (let level = 1; level <= up.level; level++) {
          refund += costOfLevel(def.cost, level);
        }

        up.level = 0;
        up.nextCost = costOfLevel(def.cost, 1);
      }

      // Step 3: optimistically update character state
      this.state.updateCharacter({
        ...character,
        soulstones: character.soulstones + refund,
      });

      // Step 4: call the backend to persist the reset
      this.soulstoneUpgradeService.resetSoulstoneUpgrades().subscribe({
        next: () => {},
        error: (err) => {
          console.error('Reset failed on backend:', err);
          // Optionally: rollback or notify user
        },
      });
    });
  }
}
export function costOfLevel(c: CostCurve, level: number): number {
  if (level <= 0) throw new RangeError('Level must be >= 1');

  if (c.incrementCap == null) {
    // Simple linear: base + (level - 1) * increment
    return c.base + (level - 1) * c.increment;
  }

  const cap = c.incrementCap;
  if (level <= cap) return level;

  return cap;
}
