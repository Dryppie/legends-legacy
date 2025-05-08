import { NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { InventoryItem } from '../../models/inventoryItem';
import { InventoryItemComponent } from '../inventory-item/inventory-item.component';

@Component({
  selector: 'app-inventory-slot',
  standalone: true,
  imports: [InventoryItemComponent, NgIf],
  templateUrl: './inventory-slot.component.html',
  styleUrl: './inventory-slot.component.css',
})
export class InventorySlotComponent {
  @Input() inventoryItem!: InventoryItem;
}
