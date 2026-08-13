import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, effect, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { GameEventService } from '../../../core/services/real-time/game-event.service';
import { GameRealtimeStore } from '../../../core/services/real-time/game-realtime/game-realtime-store.service';
import { isGameRealtimeEnabled } from '../../../core/services/real-time/game-realtime/game-realtime-feature';
import { InventoryItem } from '../../../shared/models/inventoryItem';
import { ItemComponent } from '../../../shared/components/item/item.component';
import { LocalStorageService } from '../../../core/services/client-side/local-storage/local-storage.service';
import { LootHistoryEntry } from '../../../shared/models/loot-history';
import { LootHistoryService } from '../../../core/services/api/loot-history/loot-history.service';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';

export function lootHistoryLocationLabel(
  entry: Pick<LootHistoryEntry, 'source' | 'location'>,
): string {
  switch (entry.source) {
    case 'quest-reward':
      return 'Quest Rewards';
    case 'event-quest-reward':
      return 'Server-wide Event Rewards';
    case 'combat-reward':
      return entry.location?.trim() || 'Combat';
    case 'dungeon-reward':
      return entry.location?.trim() || 'Dungeon';
    case 'guild-shop':
      return entry.location?.trim() || 'Guild Shop';
    case 'champion-market':
      return entry.location?.trim() || "Champion's Market";
    case 'player-transfer':
      return entry.location?.trim()
        ? 'Trade - ' + entry.location.trim()
        : 'Trade';
    default:
      return entry.location?.trim() || 'Loot Reward';
  }
}

@Component({
  selector: 'app-loot-tracker',
  imports: [NgIf, NgFor, DatePipe, ItemComponent],
  templateUrl: './loot-tracker.component.html',
})
export class LootTrackerComponent {
  private readonly maxEntries = 50;
  entries: LootHistoryEntry[] = [];
  expanded = signal(true);
  clearing = signal(false);
  private lastLootUpdateId: string | null = null;

  constructor(
    private readonly eventService: GameEventService,
    private readonly realtimeStore: GameRealtimeStore,
    private readonly storage: LocalStorageService,
    private readonly lootHistory: LootHistoryService,
    private readonly characterActions: CharacterActionsStateService,
  ) {
    this.expanded.set(this.storage.get<boolean>('lootTrackerExpanded') ?? true);
    this.loadHistory();

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
          this.realtimeStore.addLoot(
            this.compactLoot(loot.payload),
            envelope?.occurredAt,
            loot.source ?? 'combat-reward',
            loot.location,
            loot.grantId,
          );
        }
      },
      { allowSignalWrites: true },
    );

    effect(() => {
      if (this.characterActions.resolvingOfflineProgress()) return;
      this.entries = this.realtimeStore.recentLoot();
    });
  }

  toggle() {
    this.expanded.update((v) => !v);
    this.storage.set('lootTrackerExpanded', this.expanded());
  }

  clearHistory(event: Event): void {
    event.stopPropagation();
    if (this.clearing() || this.entries.length === 0) return;

    this.clearing.set(true);
    this.lootHistory
      .clear()
      .pipe(finalize(() => this.clearing.set(false)))
      .subscribe(() => this.realtimeStore.clearLootHistory());
  }

  trackEntry(index: number, entry: LootHistoryEntry): string {
    return (
      entry.id || `${entry.item.itemInstance.id}:${entry.receivedAt}:${index}`
    );
  }

  locationLabel(entry: LootHistoryEntry): string {
    return lootHistoryLocationLabel(entry);
  }

  private loadHistory(): void {
    this.lootHistory
      .getRecent()
      .subscribe((entries) => this.realtimeStore.setLootHistory(entries));
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
