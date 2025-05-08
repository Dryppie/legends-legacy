import { Component, Input } from '@angular/core';
import { InventoryItem } from '../../models/inventoryItem';
import { ItemComponent } from '../item/item.component';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-inventory-item',
  standalone: true,
  imports: [ItemComponent, NgIf],
  templateUrl: './inventory-item.component.html',
  styleUrl: './inventory-item.component.css',
})
export class InventoryItemComponent {
  @Input() inventoryItem!: InventoryItem;
}
