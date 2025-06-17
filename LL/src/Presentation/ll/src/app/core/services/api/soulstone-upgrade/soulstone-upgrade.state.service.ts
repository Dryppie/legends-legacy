import { signal, computed, Injectable } from '@angular/core';
import { finalize } from 'rxjs';
import { SoulstoneUpgradeType } from '../../../../shared/models/soulstones/soulstone-upgrade-type';
import { SoulstoneUpgradeView } from '../../../../shared/models/soulstones/soulstone-upgrade-view';
import { SoulstoneUpgradeService } from './soulstone-upgrade.service';
import { CostCurve } from '../../../../shared/models/soulstones/cost-curve';
import { CharacterStateService } from '../character/character-state.service';

@Injectable({
  providedIn: 'root',
})
export class SoulstoneUpgradeStateService {
  private readonly _upgrades = signal<SoulstoneUpgradeView[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _lastRefund = signal(0); // Optional: Expose latest refund for UI or sync

  readonly upgrades = computed(() => this._upgrades());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly lastRefund = computed(() => this._lastRefund());

  constructor(
    private readonly service: SoulstoneUpgradeService,
    private readonly characterState: CharacterStateService,
  ) {}

  load(): void {
    if (this._upgrades().length > 0) return;

    this._loading.set(true);
    this.service
      .getSoulstoneUpgrades()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (list) => this._upgrades.set(list),
        error: (err) =>
          this._error.set(err.message ?? 'Failed to load upgrades'),
      });
  }

  upgrade(id: string): void {
    if (this._loading()) return;

    const upgrades = this._upgrades();
    const index = upgrades.findIndex((u) => u.definition.id === id);
    if (index === -1) return;

    const up = upgrades[index];
    const cost = up.nextCost;
    const character = this.characterState.currentCharacter();

    if (!character || cost == null || character.soulstones < cost) return;

    this._loading.set(true);
    this.service
      .upgrade(id)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (success) => {
          if (!success) return;

          const updated = [...upgrades];
          const def = up.definition;
          const nextLevel = up.level + 1;

          let nextCost: number | undefined = cost + def.cost.increment;
          if (def.cost.incrementCap && nextCost > def.cost.incrementCap)
            nextCost = def.cost.incrementCap;

          if (nextLevel > def.maxLevel) nextCost = undefined;

          updated[index] = {
            ...up,
            level: nextLevel,
            nextCost,
          };

          this._upgrades.set(updated);

          this.characterState.updateCharacter({
            ...character,
            soulstones: character.soulstones - cost,
          });
        },
        error: (err) => console.error(`Upgrade '${id}' failed`, err),
      });
  }

  reset(): void {
    if (this._loading()) return;

    const current = this.characterState.currentCharacter();
    if (!current) return;

    const { refund, newList } = this.computeReset();

    this._loading.set(true);
    this.service
      .resetSoulstoneUpgrades()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: () => {
          this._upgrades.set(newList);
          this._lastRefund.set(refund);

          this.characterState.updateCharacter({
            ...current,
            soulstones: current.soulstones + refund,
          });
        },
        error: (err) => {
          console.error('Reset failed on backend:', err);
          this._error.set('Reset failed');
        },
      });
  }

  private computeReset(): { refund: number; newList: SoulstoneUpgradeView[] } {
    let refund = 0;
    const newList = this._upgrades().map((u) => {
      for (let lvl = 1; lvl <= u.level; lvl++) {
        refund += costOfLevel(u.definition.cost, lvl);
      }

      return {
        ...u,
        level: 0,
        nextCost: costOfLevel(u.definition.cost, 1),
      };
    });

    return { refund, newList };
  }

  // --- categorized computed views ---
  readonly combatUpgrades = computed(() =>
    this._upgrades().filter(
      (u) => u.definition.type === SoulstoneUpgradeType.Combat,
    ),
  );

  readonly gatheringUpgrades = computed(() =>
    this._upgrades().filter(
      (u) => u.definition.type === SoulstoneUpgradeType.Gathering,
    ),
  );

  readonly craftingUpgrades = computed(() =>
    this._upgrades().filter(
      (u) => u.definition.type === SoulstoneUpgradeType.Crafting,
    ),
  );

  readonly miscUpgrades = computed(() =>
    this._upgrades().filter(
      (u) => u.definition.type === SoulstoneUpgradeType.Misc,
    ),
  );
}

export function costOfLevel(c: CostCurve, level: number): number {
  if (level <= 0) throw new RangeError('Level must be >= 1');

  if (c.incrementCap == null) {
    return c.base + (level - 1) * c.increment;
  }

  const cap = c.incrementCap;
  if (level <= cap) return level;

  return cap;
}
