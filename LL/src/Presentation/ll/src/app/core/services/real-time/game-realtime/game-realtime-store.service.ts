import { Injectable, computed, effect, signal, untracked } from '@angular/core';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { LootHistoryEntry } from '../../../../shared/models/loot-history';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';

@Injectable({ providedIn: 'root' })
export class GameRealtimeStore {
  private readonly maxLootEntries = 50;
  private readonly _recentLoot = signal<LootHistoryEntry[]>([]);
  private readonly _lastIdleAction = signal<CharacterActionDto | null>(null);
  private readonly _lastRewardClaim = signal<InventoryItem[]>([]);
  private readonly processedLootGrantIds = new Set<string>();
  private readonly processedLootGrantOrder: string[] = [];

  readonly recentLoot = computed(() => this._recentLoot());
  readonly lastIdleAction = computed(() => this._lastIdleAction());
  readonly lastRewardClaim = computed(() => this._lastRewardClaim());

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
    if (grantId && !this.markLootGrantProcessed(grantId)) return;

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

  setRewardClaim(
    items: InventoryItem[],
    receivedAt?: string,
    source = 'dungeon-reward',
    location?: string | null,
  ): void {
    this._lastRewardClaim.set(items.slice(0, this.maxLootEntries));
    this.addLoot(items, receivedAt, source, location);
  }

  setIdleAction(action: CharacterActionDto): void {
    this._lastIdleAction.set(action);
  }

  clear(): void {
    this.clearLootHistory();
    this._lastIdleAction.set(null);
    this._lastRewardClaim.set([]);
    this.processedLootGrantIds.clear();
    this.processedLootGrantOrder.length = 0;
  }

  clearLootHistory(): void {
    this._recentLoot.set([]);
  }

  private markLootGrantProcessed(grantId: string): boolean {
    if (this.processedLootGrantIds.has(grantId)) {
      return false;
    }

    this.processedLootGrantIds.add(grantId);
    this.processedLootGrantOrder.push(grantId);
    while (this.processedLootGrantOrder.length > 500) {
      const expired = this.processedLootGrantOrder.shift();
      if (expired) this.processedLootGrantIds.delete(expired);
    }

    return true;
  }
}
