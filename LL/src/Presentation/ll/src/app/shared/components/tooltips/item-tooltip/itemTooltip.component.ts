import { Component, Input } from '@angular/core';
import {
  Equipment,
  EquipmentInstance,
  EssenceItem,
  ItemInstance,
} from '../../../models/item';
import { NgClass, NgIf } from '@angular/common';
import { Rarity } from '../../../models/enums/rarity';
import { ItemType } from '../../../models/enums/itemType';
import { ModalService } from '../../../../core/services/client-side/modal/modal.service';

@Component({
  selector: 'app-item-tooltip',
  standalone: true,
  imports: [NgClass, NgIf],
  templateUrl: './itemTooltip.component.html',
  styleUrl: './itemTooltip.component.css',
})
export class ItemTooltipComponent {
  @Input() item!: ItemInstance;
  @Input() title!: string;
  @Input() rarity!: Rarity;
  @Input() itemType!: ItemType;
  @Input() description!: string;

  constructor(private modalService: ModalService) {}

  get rarityClasses() {
    switch (this.item.itemBase.rarity) {
      case Rarity.Common:
        return 'border border-gray-600';
      case Rarity.Rare:
        return 'border border-blue-600';
      case Rarity.Unique:
        return 'border border-yellow-400';
      case Rarity.Legendary:
        return 'border border-orange-600';
      case Rarity.Legacy:
        return 'border border-red-600';
      default:
        return 'border-gray-600';
    }
  }

  isEquipment(): any {
    return this.itemType === ItemType.Equipment;
  }

  openEquipItemModal() {
    this.modalService.toggleInventoryEquipItemModal(
      this.item as EquipmentInstance,
    );
  }

  isEssence() {
    return this.itemType === ItemType.Essence;
  }

  openEssenceModal() {
    this.modalService.toggleEssenceModal(
      (this.item.itemBase as EssenceItem).essence,
    ); // Pass the essence from the Item to display all necessary info
  }
}
