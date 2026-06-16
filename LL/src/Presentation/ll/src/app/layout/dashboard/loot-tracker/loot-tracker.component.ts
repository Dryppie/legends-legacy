import { NgFor, NgIf } from '@angular/common';
import { Component, effect, signal } from '@angular/core';
import { GameEventService } from '../../../core/services/real-time/game-event.service';
import { InventoryItem } from '../../../shared/models/inventoryItem';
import { ItemComponent } from '../../../shared/components/item/item.component';
import { LocalStorageService } from '../../../core/services/client-side/local-storage/local-storage.service';

@Component({
  selector: 'app-loot-tracker',
  standalone: true,
  imports: [NgIf, NgFor, ItemComponent],
  templateUrl: './loot-tracker.component.html',
})
export class LootTrackerComponent {
  entries: InventoryItem[] = [];
  expanded = signal(true);
  private lastLootUpdateId: string | null = null;

  constructor(
    private readonly eventService: GameEventService,
    private readonly storage: LocalStorageService,
  ) {
    this.expanded.set(this.storage.get<boolean>('lootTrackerExpanded') ?? true);

    effect(
      () => {
        const envelope = this.eventService.eventEnvelope.LootReceivedMsg();
        const loot = envelope?.payload;
        if (loot) {
          const updateId = envelope?.updateId ?? envelope?.occurredAt ?? null;
          if (updateId && updateId === this.lastLootUpdateId) {
            return;
          }

          this.lastLootUpdateId = updateId;
          loot.payload.forEach((item) => {
            this.entries.push(item);
          });
        }
      },
      { allowSignalWrites: true },
    );
  }

  toggle() {
    this.expanded.update((v) => !v);
    this.storage.set('lootTrackerExpanded', this.expanded());
  }
}
