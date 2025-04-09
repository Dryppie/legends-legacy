import { NgFor } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { TabComponent } from '../../../../shared/components/tab/tab.component';
import { Tab } from '../../../../shared/models/sidebar-item';
import { InventorySlotComponent } from '../../../../shared/components/inventory-slot/inventory-slot.component';
import { InventoryService } from '../../../../core/services/api/inventory/inventory.service';
import { InventoryDto } from '../../../../shared/models/Dtos/inventoryDto';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { InventoryItem } from '../../../../shared/models/inventoryItem';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [
    NgFor,
    TabComponent,
    InventorySlotComponent,
    DefaultHeaderComponent,
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
  items: InventoryItem[] = [];
  emptySlots = Array(180).fill(null);

  constructor(private inventoryService: InventoryService) {}

  ngOnInit(): void {
    this.getInventory();
    this.setActiveTab(this.tabs[0]?.label || '');
  }

  getInventory(): void {
    this.inventoryService.getInventory().subscribe({
      next: (inventory: InventoryDto) => {
        // Update the component's items
        this.items = inventory.inventoryItems;
        // Adjust the number of empty slots based on the items
        this.emptySlots = Array(180 - this.items.length).fill(null);
      },
      error: (error) => {
        console.error('Error fetching inventory:', error);
      },
    });
  }

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }

  get filteredItems(): InventoryItem[] {
    switch (this.activeTab) {
      case 'All':
        return this.items;

      case 'Equipment':
        return this.items.filter(
          (inventoryItem) =>
            inventoryItem.itemInstance.itemBase.itemType === 'Equipment',
        );

      case 'Resources':
        return this.items.filter(
          (inventoryItem) =>
            inventoryItem.itemInstance.itemBase.itemType === 'Material',
        );

      case 'Essences':
        return this.items.filter(
          (inventoryItem) =>
            inventoryItem.itemInstance.itemBase.itemType === 'Essence',
        );

      default:
        // Fallback if no matching case; you can decide what makes sense
        // E.g., return an empty array, or return all items
        return this.items;
    }
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }
}
