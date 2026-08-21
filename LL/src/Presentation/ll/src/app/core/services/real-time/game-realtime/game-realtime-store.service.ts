import { Injectable, computed, effect, signal, untracked } from '@angular/core';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { LootHistoryEntry } from '../../../../shared/models/loot-history';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { BusinessGrantDeduper } from './realtime-deduplication';

@Injectable({ providedIn: 'root' })
export class GameRealtimeStore {
  private readonly maxLootEntries = 50;
  private readonly _recentLoot = signal<LootHistoryEntry[]>([]);
  private readonly lootGrantDeduper = new BusinessGrantDeduper();

  readonly recentLoot = computed(() => this._recentLoot());

  constructor(private readonly eventBus: EventBusService) {
    effect(
      () => {
        if (this.eventBus.logout()) {
          untracked(() => this.clear());
        }
      },
      { allowSignalWrites: true },
    );
  }

  setLootHistory(entries: LootHistoryEntry[]): void {
    this._recentLoot.set(entries.slice(0, this.maxLootEntries));
  }

  addLoot(
    items: InventoryItem[],
    receivedAt = new Date().toISOString(),
    source = 'loot',
    location?: string | null,
    grantId?: string | null,
  ): void {
    if (!items.length) return;
    if (!this.lootGrantDeduper.shouldApply(grantId)) return;

    const entries = items.map((item, index) => ({
      id: `live:${receivedAt}:${item.itemInstance.id}:${index}`,
      item,
      receivedAt,
      source,
      location,
    }));
    this._recentLoot.update((current) =>
      [...entries, ...current].slice(0, this.maxLootEntries),
    );
  }

  clear(): void {
    this.clearLootHistory();
    this.lootGrantDeduper.clear();
  }

  clearLootHistory(): void {
    this._recentLoot.set([]);
  }
}
