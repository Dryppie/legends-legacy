import { NgFor } from '@angular/common';
import { Component, EventEmitter, Output, signal } from '@angular/core';
import { ItemType } from '../../../models/enums/itemType';
import {
  DropdownComponent,
  DropdownSelection,
} from '../../custom-components/dropdown/dropdown.component';

export interface ItemTypeSelection {
  itemType: ItemType;
  subcategory: string | null;
}

@Component({
  selector: 'app-market-place-filter',
  standalone: true,
  imports: [NgFor, DropdownComponent],
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
  readonly selectedSubCategory = signal<string | null>(null);

  @Output() readonly itemTypeChanged = new EventEmitter<
    DropdownSelection<ItemType>
  >();

  readonly itemTypeSubcategories: Record<ItemType, string[]> = {
    [ItemType.Resource]: ['Wood', 'Ore'],
    [ItemType.Consumable]: ['Potion', 'Food', 'Scroll'],
    [ItemType.Equipment]: [],
    [ItemType.Essence]: [],
  };

  onSelection(sel: DropdownSelection<ItemType>): void {
    this.selectedItemType.set(sel.main);
    this.selectedSubCategory.set(sel.sub);
    this.itemTypeChanged.emit(sel);
  }
}
