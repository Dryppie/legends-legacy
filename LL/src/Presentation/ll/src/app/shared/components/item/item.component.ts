import { Component, Input } from '@angular/core';
import { ItemInstance } from '../../models/item';
import { NgClass, NgIf } from '@angular/common';
import { Rarity } from '../../models/enums/rarity';

@Component({
  selector: 'app-item',
  standalone: true,
  imports: [NgClass],
  templateUrl: './item.component.html',
  styleUrl: './item.component.css',
})
export class ItemComponent {
  @Input() item!: ItemInstance;
  itemHovered: boolean = false;
  tooltipPosition = {};

  get rarityClasses() {
    switch (this.item.itemBase.rarity) {
      case Rarity.Common:
        return 'text-white';
      case Rarity.Rare:
        return ' text-blue-600';
      case Rarity.Unique:
        return ' text-yellow-400';
      case Rarity.Legendary:
        return ' text-orange-600';
      case Rarity.Legacy:
        return ' text-red-600';
      default:
        return 'text-gray-600';
    }
  }
}
