import { NgFor, NgIf } from '@angular/common';
import { Component, effect, signal } from '@angular/core';
import { GameEventService } from '../../../core/services/real-time/game-event.service';
import { InventoryItem } from '../../../shared/models/inventoryItem';
import { ItemComponent } from '../../../shared/components/item/item.component';

@Component({
  selector: 'app-loot-tracker',
  standalone: true,
  imports: [NgIf, NgFor, ItemComponent],
  templateUrl: './loot-tracker.component.html',
})
export class LootTrackerComponent {
  entries: InventoryItem[] = [];
  expanded = signal(true);

  constructor(private readonly eventService: GameEventService) {
    // Example data
    effect(
      () => {
        const loot = this.eventService.event.LootReceivedMsg();
        if (loot) {
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
  }
}
