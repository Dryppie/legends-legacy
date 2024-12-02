import { Component, HostListener, Input } from '@angular/core';
import { Item } from '../../models/item';
import { NgIf } from '@angular/common';
import { TooltipComponent } from '../tooltip/tooltip.component';

@Component({
  selector: 'app-item',
  standalone: true,
  imports: [TooltipComponent, NgIf],
  templateUrl: './item.component.html',
  styleUrl: './item.component.css',
})
export class ItemComponent {
  @Input() item!: Item;
  itemHovered: boolean = false;
  tooltipPosition = {};

  showTooltip() {
    this.itemHovered = true;
  }

  hideTooltip() {
    this.itemHovered = false;
  }
}
