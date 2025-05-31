import { NgFor } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { TabComponent } from '../../../../shared/components/tab/tab.component';
import { Tab } from '../../../../shared/models/sidebar-item';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { EquipmentOverviewComponent } from '../../../../shared/components/equipment-overview/equipment-overview.component';
import { InventoryItemComponent } from '../../../../shared/components/inventory-item/inventory-item.component';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [
    NgFor,
    TabComponent,
    InventoryItemComponent,
    DefaultHeaderComponent,
    EquipmentOverviewComponent,
  ],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.css',
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

  constructor(public state: InventoryStateService) {}

  ngOnInit(): void {
    this.state.load();
    this.setActiveTab(this.tabs[0]?.label || '');
  }

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }

  get filteredItems(): InventoryItem[] {
    switch (this.activeTab) {
      case 'All':
        return this.state.items();

      case 'Equipment':
        return this.state.equipment();

      case 'Resources':
        return this.state.materials();

      case 'Essences':
        return this.state.essences();

      default:
        return this.state.items();
    }
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }
}
