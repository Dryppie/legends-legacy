import { Component, Input } from '@angular/core';
import { ItemInstance } from '../../models/item';
import { NgIf } from '@angular/common';
import { ItemTooltipComponent } from '../tooltips/item-tooltip/itemTooltip.component';

@Component({
  selector: 'app-item',
  standalone: true,
  imports: [NgIf, ItemTooltipComponent],
  templateUrl: './item.component.html',
  styleUrl: './item.component.css',
})
export class ItemComponent {
  @Input() item!: ItemInstance;
  itemHovered: boolean = false;
  tooltipPosition = {};

  showTooltip() {
    this.itemHovered = true;
  }

  hideTooltip() {
    this.itemHovered = false;
  }
}
