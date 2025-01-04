import { Component, Input } from '@angular/core';
import { EssenceItem, Item } from '../../../models/item';
import { NgClass, NgIf } from '@angular/common';
import { Rarity } from '../../../models/enums/rarity';
import { ItemType } from '../../../models/enums/itemType';
import { ModalService } from '../../../../core/services/modal/modal.service';
import { EssencesService } from '../../../../core/services/essences/essences.service';

@Component({
  selector: 'app-item-tooltip',
  standalone: true,
  imports: [NgClass, NgIf],
  templateUrl: './itemTooltip.component.html',
  styleUrl: './itemTooltip.component.css',
})
export class ItemTooltipComponent {
  @Input() item!: Item;
  @Input() title!: string;
  @Input() rarity!: Rarity;
  @Input() itemType!: ItemType;
  @Input() description!: string;

  constructor(
    private modalService: ModalService,
    private essencesService: EssencesService,
  ) {}

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

  isEssence() {
    return this.itemType === ItemType.Essence;
  }

  openEssenceModal() {
    this.modalService.toggleEssenceModal((this.item as EssenceItem).essence); // Pass the essence from the Item to display all necessary info
  }

  equipEssence() {
    this.essencesService.equipEssence('00000000-0000-0000-0000-000000000001');
  }
}
