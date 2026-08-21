import { NgFor, NgIf } from '@angular/common';
import { Component, effect, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { GameRealtimeStore } from '../../../core/services/real-time/game-realtime/game-realtime-store.service';
import { ItemComponent } from '../../../shared/components/item/item.component';
import { LocalStorageService } from '../../../core/services/client-side/local-storage/local-storage.service';
import { LootHistoryEntry } from '../../../shared/models/loot-history';
import { LootHistoryService } from '../../../core/services/api/loot-history/loot-history.service';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';
import { LocalDatePipe } from '../../../shared/pipes/local-date/local-date.pipe';

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
    case 'tournament-reward':
      return entry.location?.trim() || 'Tournament Grounds';
    case 'guild-shop':
      return entry.location?.trim() || 'Guild Shop';
    case 'champion-market':
      return entry.location?.trim() || "Champion's Market";
    case 'container-reward':
      return entry.location?.trim()
        ? 'Opened: ' + entry.location.trim()
        : 'Opened Item';
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
  imports: [NgIf, NgFor, LocalDatePipe, ItemComponent],
  templateUrl: './loot-tracker.component.html',
})
export class LootTrackerComponent {
  private readonly maxEntries = 50;
  entries: LootHistoryEntry[] = [];
  expanded = signal(true);
  clearing = signal(false);
  constructor(
    private readonly realtimeStore: GameRealtimeStore,
    private readonly storage: LocalStorageService,
    private readonly lootHistory: LootHistoryService,
    private readonly characterActions: CharacterActionsStateService,
  ) {
    this.expanded.set(this.storage.get<boolean>('lootTrackerExpanded') ?? true);
    this.loadHistory();

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

}
