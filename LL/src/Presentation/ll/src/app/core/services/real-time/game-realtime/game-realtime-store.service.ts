import { Injectable, computed, signal } from '@angular/core';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { InventoryItem } from '../../../../shared/models/inventoryItem';

interface RecentLootEntry {
  item: InventoryItem;
  receivedAt: number;
}

@Injectable({ providedIn: 'root' })
export class GameRealtimeStore {
  private readonly maxLootEntries = 60;
  private readonly _recentLoot = signal<RecentLootEntry[]>([]);
  private readonly _lastIdleAction = signal<CharacterActionDto | null>(null);
  private readonly _lastRewardClaim = signal<InventoryItem[]>([]);

  readonly recentLoot = computed(() => this._recentLoot());
  readonly lastIdleAction = computed(() => this._lastIdleAction());
  readonly lastRewardClaim = computed(() => this._lastRewardClaim());

  addLoot(items: InventoryItem[]): void {
    if (!items.length) return;

    const entries = items.map((item) => ({
      item,
      receivedAt: Date.now(),
    }));
    this._recentLoot.update((current) =>
      [...current, ...entries].slice(-this.maxLootEntries),
    );
  }

  setRewardClaim(items: InventoryItem[]): void {
    this._lastRewardClaim.set(items.slice(0, this.maxLootEntries));
    this.addLoot(items);
  }

  setIdleAction(action: CharacterActionDto): void {
    this._lastIdleAction.set(action);
  }

  clear(): void {
    this._recentLoot.set([]);
    this._lastIdleAction.set(null);
    this._lastRewardClaim.set([]);
  }
}
