import { Component, Input } from '@angular/core';
import { Item } from '../../models/item';
import { NgClass, NgFor, NgStyle } from '@angular/common';
import { Rarity } from '../../models/enums/Rarity';

@Component({
  selector: 'app-tooltip',
  standalone: true,
  imports: [NgFor, NgClass],
  templateUrl: './tooltip.component.html',
  styleUrl: './tooltip.component.css',
})
export class TooltipComponent {
  @Input() item!: Item;

  get rarityClasses() {
    switch (this.item.rarity) {
      case Rarity.Common:
        return 'bg-gray-700 border border-gray-600';
      case Rarity.Rare:
        return 'bg-green-700 border border-green-600';
      case Rarity.Unique:
        return 'bg-blue-700 border border-blue-600';
      case Rarity.Legendary:
        return 'bg-purple-700 border border-purple-600';
      case Rarity.Legacy:
        return 'bg-yellow-700 border border-yellow-600';
      default:
        return 'bg-gray-800';
    }
  }
}
