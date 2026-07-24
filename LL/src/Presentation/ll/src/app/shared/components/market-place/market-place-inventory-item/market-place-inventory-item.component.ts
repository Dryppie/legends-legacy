import { Component, Input } from '@angular/core';
import { InventoryItem } from '../../../models/inventoryItem';
import { NgIf } from '@angular/common';
import { ItemType } from '../../../models/enums/itemType';
import { ItemComponent } from '../../item/item.component';
import { NumberFormatPipe } from '../../../pipes/number-format/number-format.pipe';

@Component({
    selector: 'app-market-place-inventory-item',
    imports: [ItemComponent, NgIf, NumberFormatPipe],
    templateUrl: './market-place-inventory-item.component.html'
})
export class MarketPlaceInventoryItemComponent {
  @Input() inventoryItem!: InventoryItem;

  get isEquipment(): boolean {
    return (
      this.inventoryItem.itemInstance.itemBase.itemType === ItemType.Equipment
    );
  }

}
