import { Component, Input } from '@angular/core';
import { InventoryItem } from '../../models/inventoryItem';
import { DecimalPipe, NgIf } from '@angular/common';
import { ModalService } from '../../../core/services/client-side/modal/modal.service';
import { ItemType } from '../../models/enums/itemType';
import { EquipmentInstance } from '../../models/item';
import { ItemComponent } from '../item/item.component';

@Component({
  selector: 'app-inventory-item',
  imports: [ItemComponent, NgIf, DecimalPipe],
  templateUrl: './inventory-item.component.html',
})
export class InventoryItemComponent {
  @Input() inventoryItem!: InventoryItem;
  @Input() showEquipmentSummary = false;

  constructor(private modal: ModalService) {}

  get isEquipment(): boolean {
    return (
      this.inventoryItem.itemInstance.itemBase.itemType === ItemType.Equipment
    );
  }

  openModal(): void {
    if (this.isEquipment) {
      this.modal.toggleInventoryEquipItemModal(
        this.inventoryItem.itemInstance as EquipmentInstance,
      );
    } else {
      this.modal.toggleInventoryItemModal(this.inventoryItem);
    }
  }

  get equipment(): EquipmentInstance | null {
    return this.isEquipment
      ? (this.inventoryItem.itemInstance as EquipmentInstance)
      : null;
  }
}
