import { NgFor } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { TabComponent } from '../../../../shared/components/tab/tab.component';
import { Tab } from '../../../../shared/models/sidebar-item';
import { InventorySlotComponent } from '../../../../shared/components/inventory-slot/inventory-slot.component';
import { InventoryService } from '../../../../core/services/inventory/inventory.service';
import { InventoryDto } from '../../../../shared/models/Dtos/inventoryDto';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { EssenceItem } from '../../../../shared/models/item';

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
      label: 'Other',
      items: [],
    },
  ];
  activeTab: string = '';
  items: InventoryItem[] = [];
  emptySlots = Array(180).fill(null);

  constructor(private inventoryService: InventoryService) {}

  ngOnInit(): void {
    this.getInventory();
  }

  getInventory(): void {
    this.inventoryService.getInventory().subscribe({
      next: (inventory: InventoryDto) => {
        // Update the component's items
        this.items = inventory.inventoryItems;
        let test = this.items[0];
        let test1 = test.item;
        console.log(test1 as EssenceItem);
        // Adjust the number of empty slots based on the items
        this.emptySlots = Array(180 - this.items.length).fill(null);
      },
      error: (error) => {
        console.error('Error fetching inventory:', error);
      },
    });
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }
}
