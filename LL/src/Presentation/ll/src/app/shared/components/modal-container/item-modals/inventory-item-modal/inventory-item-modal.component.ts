import { NgIf } from '@angular/common';
import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { CraftingService } from '../../../../../core/services/api/crafting/crafting.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { InventoryItem } from '../../../../models/inventoryItem';
import { ItemComponent } from '../../../item/item.component';

@Component({
  selector: 'app-inventory-item-modal',
  standalone: true,
  imports: [NgIf, ItemComponent],
  templateUrl: './inventory-item-modal.component.html',
})
export class InventoryItemModalComponent {
  @Input({ required: true }) inventoryItem!: InventoryItem;
  @Output() close = new EventEmitter<void>();
  readonly isLearning = signal(false);
  readonly error = signal<string | null>(null);

  constructor(
    private readonly craftingService: CraftingService,
    private readonly inventoryState: InventoryStateService,
  ) {}

  get itemName(): string {
    return (
      this.inventoryItem.itemInstance.displayName ||
      this.inventoryItem.itemInstance.itemBase.name
    );
  }

  get blueprint() {
    return this.inventoryItem.itemInstance.itemBase.blueprint ?? null;
  }

  learnBlueprint(): void {
    if (!this.blueprint || this.isLearning()) return;
    this.isLearning.set(true);
    this.error.set(null);
    this.craftingService
      .learnBlueprint(this.inventoryItem.itemInstance.id)
      .subscribe({
        next: () => {
          this.inventoryState.decrementItem(
            this.inventoryItem.itemInstance.id,
            1,
          );
          this.close.emit();
        },
        error: (err) => {
          this.error.set(err.message ?? 'Failed to learn blueprint.');
          this.isLearning.set(false);
        },
      });
  }
}
