import { Component, Input } from '@angular/core';
import { InventoryItem } from '../../models/inventoryItem';
import { ItemComponent } from '../item/item.component';
import { NgIf } from '@angular/common';
import { ModalService } from '../../../core/services/client-side/modal/modal.service';
import { ItemType } from '../../models/enums/itemType';
import { Equipment, EquipmentInstance, EssenceItem } from '../../models/item';
import { EquipmentType } from '../../models/enums/equipmentType';

@Component({
  selector: 'app-inventory-item',
  standalone: true,
  imports: [ItemComponent, NgIf],
  templateUrl: './inventory-item.component.html',
})
export class InventoryItemComponent {
  @Input() inventoryItem!: InventoryItem;

  constructor(private modal: ModalService) {}

  get isEquipment(): boolean {
    return (
      this.inventoryItem.itemInstance.itemBase.itemType === ItemType.Equipment
    );
  }

  get isEssence(): boolean {
    return (
      this.inventoryItem.itemInstance.itemBase.itemType === ItemType.Essence
    );
  }

  get equipmentIcon(): string | null {
    if (!this.isEquipment) return null;
    const equipmentType = (
      this.inventoryItem.itemInstance.itemBase as Equipment
    ).equipmentType;
    if (
      equipmentType === EquipmentType.TwoHanded ||
      equipmentType === EquipmentType.OneHanded
    )
      return 'mainhand';
    else return equipmentType.toLowerCase();
  }

  get equipmentIconPath(): string | null {
    return this.equipmentIcon
      ? `icons/equipment-slots/empty_${this.equipmentIcon}.svg`
      : null;
  }

  openModal(): void {
    if (this.isEquipment) {
      this.modal.toggleInventoryEquipItemModal(
        this.inventoryItem.itemInstance as EquipmentInstance,
      );
    } else if (this.isEssence) {
      const essence = (this.inventoryItem.itemInstance.itemBase as EssenceItem)
        .essence;
      this.modal.toggleEssenceModal(essence);
    }
  }
}
