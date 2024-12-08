import { Component, Input } from '@angular/core';
import { Item } from '../../../models/item';
import { NgClass, NgFor, NgStyle } from '@angular/common';
import { Rarity } from '../../../models/enums/rarity';
import { ItemType } from '../../../models/enums/itemType';

@Component({
  selector: 'app-item-tooltip',
  standalone: true,
  imports: [NgClass],
  templateUrl: './itemTooltip.component.html',
  styleUrl: './itemTooltip.component.css',
})
export class ItemTooltipComponent {
  @Input() item!: Item;
  @Input() title!: string;
  @Input() rarity!: Rarity;
  @Input() itemType!: ItemType;
  @Input() description!: string;

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
