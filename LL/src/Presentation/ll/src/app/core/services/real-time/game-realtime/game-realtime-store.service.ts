import { Injectable, computed, effect, signal, untracked } from '@angular/core';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { LootHistoryEntry } from '../../../../shared/models/loot-history';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { BusinessGrantDeduper } from './realtime-deduplication';

@Injectable({ providedIn: 'root' })
export class GameRealtimeStore {
  private readonly maxLootEntries = 50;
  private readonly authoritativeLootMatchWindowMs = 60_000;
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

    const authoritativeEntries = this._recentLoot().filter(
      (entry) => !entry.id.startsWith('live:'),
    );
    const entries = items
      .filter((item) => {
        const matchIndex = authoritativeEntries.findIndex((entry) =>
          this.matchesAuthoritativeEntry(
            entry,
            item,
            receivedAt,
            source,
            location,
          ),
        );
        if (matchIndex < 0) return true;

        authoritativeEntries.splice(matchIndex, 1);
        return false;
      })
      .map((item, index) => ({
        id: `live:${receivedAt}:${item.itemInstance.id}:${index}`,
        item,
        receivedAt,
        source,
        location,
      }));
    if (!entries.length) return;

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

  private matchesAuthoritativeEntry(
    entry: LootHistoryEntry,
    item: InventoryItem,
    receivedAt: string,
    source: string,
    location?: string | null,
  ): boolean {
    if (entry.item.itemInstance.id !== item.itemInstance.id) return false;
    if (entry.item.quantity !== item.quantity) return false;
    if (entry.source !== source) return false;
    if ((entry.location?.trim() || null) !== (location?.trim() || null)) {
      return false;
    }

    const authoritativeTime = Date.parse(entry.receivedAt);
    const liveTime = Date.parse(receivedAt);
    return (
      Number.isFinite(authoritativeTime) &&
      Number.isFinite(liveTime) &&
      Math.abs(authoritativeTime - liveTime) <=
        this.authoritativeLootMatchWindowMs
    );
  }
}
