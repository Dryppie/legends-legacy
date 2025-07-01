import { NgFor, NgIf } from '@angular/common';
import { Component, computed, OnInit } from '@angular/core';
import { Tab } from '../../../../shared/models/sidebar-item';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { EquipmentOverviewComponent } from '../../../../shared/components/equipment-overview/equipment-overview.component';
import { InventoryItemComponent } from '../../../../shared/components/inventory-item/inventory-item.component';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { FilterTabsComponent } from '../../../../shared/components/tabs/filter-tabs/filter-tabs.component';
import { RegularButtonComponent } from '../../../../shared/components/buttons/regular-button/regular-button.component';
import { EquipmentInstance } from '../../../../shared/models/item';
import { Rarity } from '../../../../shared/models/enums/rarity';
import { FormsModule } from '@angular/forms';
import { ItemComponent } from '../../../../shared/components/item/item.component';
import { HelpTooltipDirective } from '../../../../shared/help/help-tooltip.directive';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    FilterTabsComponent,
    InventoryItemComponent,
    DefaultHeaderComponent,
    EquipmentOverviewComponent,
    RegularButtonComponent,
    FormsModule,
    ItemComponent,
    HelpTooltipDirective,
  ],
  templateUrl: './inventory.component.html',
})
export class InventoryComponent implements OnInit {
  tabs: Tab[] = [
    {
      label: 'All',
      items: [],
    },
    {
      label: 'Equipment',
      items: [],
    },
    {
      label: 'Resources',
      items: [],
    },
    {
      label: 'Essences',
      items: [],
    },
  ];
  activeTab: string = '';

  inventoryMode: 'Scrap Mode' | 'Regular Mode' = 'Regular Mode';
  variant: 'danger' | 'primary' = 'primary';

  temperedItems = computed(() => {
    return this.state
      .equipment()
      .filter((i) => (i.itemInstance as EquipmentInstance).potential === 0);
  });

  selectedItems: InventoryItem[] = [];
  scrapRarityThreshold: Rarity = Rarity.Common;
  rarities = Object.keys(Rarity);
  RARITY_ORDER: Record<Rarity, number> = {
    [Rarity.Common]: 0,
    [Rarity.Uncommon]: 1,
    [Rarity.Rare]: 2,
    [Rarity.Epic]: 3,
    [Rarity.Unique]: 4,
    [Rarity.Legendary]: 5,
    [Rarity.Legacy]: 6,
  };

  constructor(public state: InventoryStateService) {}

  ngOnInit(): void {
    this.state.load();
    this.setActiveTab(this.tabs[0]?.label || '');
  }

  toggleSelectItem(selectedItem: InventoryItem) {
    if (this.selectedItems.includes(selectedItem)) {
      this.selectedItems = this.selectedItems.filter((item) => {
        return item.itemInstance.id !== selectedItem.itemInstance.id;
      });
    } else {
      this.selectedItems.push(selectedItem);
    }
  }

  cancelScrapMode() {
    this.selectedItems = [];
    this.switchMode();
  }

  selectAllTempered() {
    this.selectedItems = [];
    this.temperedItems().forEach((item) => this.selectedItems.push(item));
  }

  selectAllBelowRarity() {
    const thresholdRank = this.RARITY_ORDER[this.scrapRarityThreshold];

    this.selectedItems = [];
    this.temperedItems()
      .filter((item) => {
        const itemRank =
          this.RARITY_ORDER[(item.itemInstance as EquipmentInstance).rarity];
        return itemRank <= thresholdRank;
      })
      .forEach((item) => this.selectedItems.push(item));
  }

  scrapEquipment() {
    this.state.scrapEquipment(this.selectedItems.map((i) => i.itemInstance.id));
    this.selectedItems = [];
  }

  switchMode() {
    this.selectedItems = [];
    this.inventoryMode =
      this.inventoryMode === 'Scrap Mode' ? 'Regular Mode' : 'Scrap Mode';
    this.variant = this.variant === 'danger' ? 'primary' : 'danger';
  }

  selectedItemsContains(item: InventoryItem) {
    return !!this.selectedItems.find(
      (i) => i.itemInstance.id === item.itemInstance.id,
    );
  }

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }

  get filteredItems(): InventoryItem[] {
    switch (
      this.inventoryMode === 'Regular Mode' ? this.activeTab : 'Equipment'
    ) {
      case 'All':
        return this.state.items();

      case 'Equipment':
        return this.inventoryMode === 'Regular Mode'
          ? this.state.equipment()
          : this.temperedItems();

      case 'Resources':
        return this.state.materials();

      case 'Essences':
        return this.state.essences();

      default:
        return this.state.items();
    }
  }

  get tabLabels(): string[] {
    return this.inventoryMode === 'Regular Mode'
      ? this.tabs.map((tab) => tab.label)
      : ['Equipment'];
  }
}
