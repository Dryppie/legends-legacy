import { NgIf } from '@angular/common';
import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { InventoryStateService } from '../../../core/services/api/inventory/inventory-state.service';
import { InventoryService } from '../../../core/services/api/inventory/inventory.service';
import { EquipmentInstance } from '../../models/item';
import { InventoryItem } from '../../models/inventoryItem';

@Component({
  selector: 'app-inventory-transfer',
  imports: [FormsModule, NgIf],
  templateUrl: './inventory-transfer.component.html',
})
export class InventoryTransferComponent {
  @Input({ required: true }) inventoryItem!: InventoryItem;
  @Output() transferred = new EventEmitter<void>();

  readonly isFormOpen = signal(false);
  readonly isTransferring = signal(false);
  readonly error = signal<string | null>(null);
  recipientName = '';
  quantity = 1;

  constructor(
    private readonly inventoryService: InventoryService,
    private readonly inventoryState: InventoryStateService,
  ) {}

  get isStackable(): boolean {
    return this.inventoryItem.itemInstance.itemBase.stackable;
  }

  get transferRestriction(): string | null {
    if (this.inventoryItem.itemInstance.itemBase.isBound) {
      return 'Bound items cannot be transferred.';
    }

    const equipment = this.inventoryItem.itemInstance as Partial<EquipmentInstance>;
    if (equipment.isGuildBorrowed) {
      return 'Borrowed guild-vault items cannot be transferred.';
    }

    return null;
  }

  openForm(): void {
    if (this.transferRestriction) return;
    this.error.set(null);
    this.quantity = 1;
    this.isFormOpen.set(true);
  }

  cancel(): void {
    if (this.isTransferring()) return;
    this.error.set(null);
    this.isFormOpen.set(false);
  }

  transfer(): void {
    const recipientName = this.recipientName.trim();
    const quantity = Math.floor(Number(this.quantity));
    if (!recipientName || !Number.isFinite(quantity) || quantity < 1) {
      this.error.set('Enter a player name and a valid quantity.');
      return;
    }
    if (quantity > this.inventoryItem.quantity) {
      this.error.set('You do not have enough of this item.');
      return;
    }

    this.isTransferring.set(true);
    this.error.set(null);
    this.inventoryService
      .transferItem(this.inventoryItem.itemInstance.id, recipientName, quantity)
      .pipe(finalize(() => this.isTransferring.set(false)))
      .subscribe({
        next: () => {
          this.inventoryState.decrementItem(
            this.inventoryItem.itemInstance.id,
            quantity,
          );
          this.transferred.emit();
        },
        error: (err) => {
          this.error.set(
            err.errorMessage ?? err.message ?? 'Failed to transfer the item.',
          );
        },
      });
  }
}
