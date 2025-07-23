import { NgClass, NgFor } from '@angular/common';
import { Component, EventEmitter, Output, signal } from '@angular/core';
import { ItemType } from '../../../models/enums/itemType';

@Component({
  selector: 'app-market-place-filter',
  standalone: true,
  imports: [NgClass, NgFor],
  templateUrl: './market-place-filter.component.html',
})
export class MarketPlaceFilterComponent {
  readonly itemTypes = [
    ItemType.Material,
    ItemType.Consumable,
    ItemType.Equipment,
    ItemType.Essence,
  ];

  readonly selectedItemType = signal<ItemType>(ItemType.Equipment);

  @Output() readonly itemTypeChanged = new EventEmitter<ItemType>();

  setSelectedType(type: ItemType) {
    this.selectedItemType.set(type);
    this.itemTypeChanged.emit(type);
  }
}
