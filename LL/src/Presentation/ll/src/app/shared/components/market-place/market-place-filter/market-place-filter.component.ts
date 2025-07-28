import { NgClass, NgFor } from '@angular/common';
import { Component, EventEmitter, Output, signal } from '@angular/core';
import { ItemType } from '../../../models/enums/itemType';

export interface ItemTypeSelection {
  itemType: ItemType;
  subcategory: string | null;
}

@Component({
  selector: 'app-market-place-filter',
  standalone: true,
  imports: [NgClass, NgFor],
  templateUrl: './market-place-filter.component.html',
})
export class MarketPlaceFilterComponent {
  readonly itemTypes = [
    ItemType.Resource,
    ItemType.Consumable,
    ItemType.Equipment,
    ItemType.Essence,
  ];

  readonly selectedItemType = signal<ItemType>(ItemType.Resource);
  readonly selectedSubcategory = signal<string | null>(null);

  @Output() readonly itemTypeChanged = new EventEmitter<ItemTypeSelection>();

  readonly itemTypeSubcategories: Record<ItemType, string[]> = {
    [ItemType.Resource]: ['Wood', 'Ore'],
    [ItemType.Consumable]: ['Potion', 'Food', 'Scroll'],
    [ItemType.Equipment]: [],
    [ItemType.Essence]: [],
  };

  setSelectedType(type: ItemType) {
    this.selectedItemType.set(type);

    const subcategories = this.itemTypeSubcategories[type];
    const defaultSub = subcategories.length > 0 ? subcategories[0] : null;
    this.selectedSubcategory.set(defaultSub);

    if (type === ItemType.Equipment || type === ItemType.Essence) {
      this.itemTypeChanged.emit({
        itemType: type,
        subcategory: defaultSub,
      });
    }
  }

  setSelectedSubcategory(sub: string) {
    this.selectedSubcategory.set(sub);
    this.itemTypeChanged.emit({
      itemType: this.selectedItemType(),
      subcategory: sub,
    });
  }

  get subcategories(): string[] {
    return this.itemTypeSubcategories[this.selectedItemType()];
  }
}
