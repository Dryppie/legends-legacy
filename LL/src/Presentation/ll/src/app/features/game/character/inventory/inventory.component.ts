import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { TabComponent } from '../../../../shared/components/tab/tab.component';
import { InventoryItem, Tab } from '../../../../shared/models/sidebar-item';
import { InventorySlotComponent } from '../../../../shared/components/inventory-slot/inventory-slot.component';
import { InventoryService } from '../../../../core/services/inventory/inventory.service';
import { InventoryDto } from '../../../../shared/models/Dtos/inventoryDto';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
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
  emptySlots = Array(120).fill(null);

  constructor(private inventoryService: InventoryService) {}

  ngOnInit(): void {
    this.getInventory();
  }

  getInventory(): void {
    this.inventoryService.getInventory().subscribe({
      next: (inventory: InventoryDto) => {
        // Update the component's items
        this.items = inventory.inventoryItems.map((item) => ({
          id: item.itemId, // Assign itemId as the id
          icon: '', // Assign icons if available or leave empty
          name: 'Unknown Item', // Replace with actual item name if available from another source
          description: '', // Add descriptions if needed
          quantity: item.quantity,
        }));
        // Adjust the number of empty slots based on the items
        this.emptySlots = Array(120 - this.items.length).fill(null);
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
