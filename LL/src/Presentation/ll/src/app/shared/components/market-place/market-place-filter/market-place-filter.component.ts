import { NgFor } from '@angular/common';
import { Component, EventEmitter, Output, signal } from '@angular/core';
import { ItemType } from '../../../models/enums/itemType';
import {
  DropdownComponent,
  DropdownSelection,
} from '../../custom-components/dropdown/dropdown.component';
import {
  MarketCategoryId,
  MarketCategorySelection,
} from '../../../models/market-category';

interface MarketPlaceFilterTab {
  id: MarketCategoryId;
  label: string;
  itemType: ItemType;
  defaultSubcategory: string | null;
  subOptions: readonly string[];
}

@Component({
  selector: 'app-market-place-filter',
  standalone: true,
  imports: [NgFor, DropdownComponent],
  templateUrl: './market-place-filter.component.html',
})
export class MarketPlaceFilterComponent {
  readonly tabs: readonly MarketPlaceFilterTab[] = [
    {
      id: 'resources',
      label: 'Resources',
      itemType: ItemType.Resource,
      defaultSubcategory: 'Metal',
      subOptions: [
        'Metal',
        'Wood',
        'Hide',
        'Crystal',
        'Stone',
        'Fiber',
        'Bone',
        'Chitin',
        'Resin',
        'Oil',
      ],
    },
    {
      id: 'consumables',
      label: 'Consumables',
      itemType: ItemType.Consumable,
      defaultSubcategory: 'Potion',
      subOptions: ['Potion', 'Food', 'Scroll'],
    },
    {
      id: 'blueprints',
      label: 'Blueprints',
      itemType: ItemType.Resource,
      defaultSubcategory: 'Blueprints',
      subOptions: [],
    },
    {
      id: 'catalysts',
      label: 'Catalysts',
      itemType: ItemType.Resource,
      defaultSubcategory: 'Catalysts',
      subOptions: [],
    },
    {
      id: 'equipment',
      label: 'Equipment',
      itemType: ItemType.Equipment,
      defaultSubcategory: null,
      subOptions: [],
    },
    {
      id: 'essences',
      label: 'Essences',
      itemType: ItemType.Essence,
      defaultSubcategory: null,
      subOptions: [],
    },
  ];

  readonly selectedTabId = signal<MarketCategoryId>('resources');
  readonly selectedSubCategory = signal<string | null>('Metal');

  @Output() readonly categoryChanged =
    new EventEmitter<MarketCategorySelection>();

  onSelection(
    tab: MarketPlaceFilterTab,
    sel: DropdownSelection<ItemType>,
  ): void {
    const subcategory = sel.sub ?? tab.defaultSubcategory;
    this.selectedTabId.set(tab.id);
    this.selectedSubCategory.set(subcategory);
    this.categoryChanged.emit({
      id: tab.id,
      label: tab.label,
      itemType: tab.itemType,
      subcategory,
    });
  }
}
