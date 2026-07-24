import { NgFor, NgIf } from '@angular/common';
import { Component, effect, signal } from '@angular/core';
import { GameEventService } from '../../../core/services/real-time/game-event.service';
import { GameRealtimeStore } from '../../../core/services/real-time/game-realtime/game-realtime-store.service';
import { isGameRealtimeEnabled } from '../../../core/services/real-time/game-realtime/game-realtime-feature';
import { InventoryItem } from '../../../shared/models/inventoryItem';
import { ItemComponent } from '../../../shared/components/item/item.component';
import { LocalStorageService } from '../../../core/services/client-side/local-storage/local-storage.service';

interface LootTrackerEntry {
  item: InventoryItem;
  receivedAt: number;
}

@Component({
    selector: 'app-loot-tracker',
    imports: [NgIf, NgFor, ItemComponent],
    templateUrl: './loot-tracker.component.html'
})
export class LootTrackerComponent {
  private readonly maxEntries = 60;
  entries: LootTrackerEntry[] = [];
  expanded = signal(true);
  private lastLootUpdateId: string | null = null;

  constructor(
    private readonly eventService: GameEventService,
    private readonly realtimeStore: GameRealtimeStore,
    private readonly storage: LocalStorageService,
  ) {
    this.expanded.set(this.storage.get<boolean>('lootTrackerExpanded') ?? true);

    effect(
      () => {
        if (isGameRealtimeEnabled()) return;

        const envelope = this.eventService.eventEnvelope.LootReceivedMsg();
        const loot = envelope?.payload;
        if (loot) {
          const updateId = envelope?.updateId ?? envelope?.occurredAt ?? null;
          if (updateId && updateId === this.lastLootUpdateId) {
            return;
          }

          this.lastLootUpdateId = updateId;
          this.entries = [
            ...this.entries,
            ...this.compactLoot(loot.payload).map((item) => ({
              item,
              receivedAt: Date.now(),
            })),
          ].slice(-this.maxEntries);
        }
      },
      { allowSignalWrites: true },
    );

    effect(() => {
      if (!isGameRealtimeEnabled()) return;
      this.entries = this.realtimeStore.recentLoot();
    });
  }

  toggle() {
    this.expanded.update((v) => !v);
    this.storage.set('lootTrackerExpanded', this.expanded());
  }

  trackEntry(index: number, entry: LootTrackerEntry): string {
    return `${entry.item.itemInstance.id}:${entry.item.itemInstance.itemBase.id}:${entry.receivedAt}:${index}`;
  }

  private compactLoot(items: InventoryItem[]): InventoryItem[] {
    const compacted: InventoryItem[] = [];
    const stackableIndexes = new Map<string, number>();

    for (const item of items) {
      const base = item.itemInstance.itemBase;
      if (!base.stackable) {
        compacted.push(item);
        continue;
      }

      const existingIndex = stackableIndexes.get(base.id);
      if (existingIndex === undefined) {
        stackableIndexes.set(base.id, compacted.length);
        compacted.push(item);
        continue;
      }

      compacted[existingIndex] = {
        ...compacted[existingIndex],
        quantity: compacted[existingIndex].quantity + item.quantity,
      };
    }

    return compacted;
  }
}
