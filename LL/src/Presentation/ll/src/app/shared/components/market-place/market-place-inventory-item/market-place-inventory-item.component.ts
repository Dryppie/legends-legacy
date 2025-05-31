import { Component, Input } from '@angular/core';
import { InventoryItem } from '../../../models/inventoryItem';
import { NgIf } from '@angular/common';
import { Equipment } from '../../../models/item';
import { ItemType } from '../../../models/enums/itemType';
import { ItemComponent } from '../../item/item.component';

@Component({
  selector: 'app-market-place-inventory-item',
  standalone: true,
  imports: [ItemComponent, NgIf],
  templateUrl: './market-place-inventory-item.component.html',
  styleUrl: './market-place-inventory-item.component.css',
})
export class MarketPlaceInventoryItemComponent {
  @Input() inventoryItem!: InventoryItem;

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
    return (
      this.inventoryItem.itemInstance.itemBase as Equipment
    ).equipmentType.toLowerCase();
  }

  get equipmentIconPath(): string | null {
    return this.equipmentIcon
      ? `icons/equipment-slots/empty_${this.equipmentIcon}.svg`
      : null;
  }
}
