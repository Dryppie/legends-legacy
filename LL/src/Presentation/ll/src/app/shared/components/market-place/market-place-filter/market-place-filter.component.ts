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

interface MarketPlaceFilterTab {
  id: string;
  label: string;
  itemType: ItemType;
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
      subOptions: ['Potion', 'Food', 'Scroll'],
    },
    {
      id: 'blueprints',
      label: 'Blueprints',
      itemType: ItemType.Resource,
      subOptions: [
        'Blueprint: Fury',
        'Blueprint: Arcane',
        'Blueprint: Execution',
        'Blueprint: Aegis',
        'Blueprint: Warden',
        'Blueprint: Endurance',
        'Blueprint: Phoenix',
        'Blueprint: Spirit',
        'Blueprint: Primal',
        'Blueprint: Venom-Touched Sword',
        'Blueprint: Hivefang Dagger',
      ],
    },
    {
      id: 'catalysts',
      label: 'Catalysts',
      itemType: ItemType.Resource,
      subOptions: [
        'Venom Gland',
        'Royal Chitin Plate',
        'Hive Ichor',
      ],
    },
    {
      id: 'equipment',
      label: 'Equipment',
      itemType: ItemType.Equipment,
      subOptions: [],
    },
    {
      id: 'essences',
      label: 'Essences',
      itemType: ItemType.Essence,
      subOptions: [],
    },
  ];

  readonly selectedTabId = signal<string>('resources');
  readonly selectedSubCategory = signal<string | null>(null);

  @Output() readonly itemTypeChanged = new EventEmitter<
    DropdownSelection<ItemType>
  >();

  onSelection(
    tab: MarketPlaceFilterTab,
    sel: DropdownSelection<ItemType>,
  ): void {
    this.selectedTabId.set(tab.id);
    this.selectedSubCategory.set(sel.sub);
    this.itemTypeChanged.emit({ main: tab.itemType, sub: sel.sub });
  }
}
