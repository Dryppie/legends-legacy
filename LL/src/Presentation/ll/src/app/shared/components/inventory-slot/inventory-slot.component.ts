import { NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { InventoryItem } from '../../models/sidebar-item';

@Component({
  selector: 'app-inventory-slot',
  standalone: true,
  imports: [NgIf],
  templateUrl: './inventory-slot.component.html',
  styleUrl: './inventory-slot.component.css',
})
export class InventorySlotComponent {
  @Input() item!: InventoryItem;
  itemHovered: boolean = false;
  showTooltip() {
    this.itemHovered = true;
  }

  hideTooltip() {
    this.itemHovered = false;
  }
}
