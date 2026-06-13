import { Component, Input } from '@angular/core';
import { EquipmentInstance, EssenceItem, ItemInstance } from '../../models/item';
import { NgClass, NgIf } from '@angular/common';
import { Rarity } from '../../models/enums/rarity';
import { EssenceDetailsComponent } from '../essences/essence-details/essence-details.component';
import { EquipmentDisplayComponent } from '../equipment/equipment-display/equipment-display.component';
import { ItemType } from '../../models/enums/itemType';
import { PopoverComponent } from '../custom-components/popover/popover.component';
import { EssenceItemViewService } from '../../../core/services/api/essences/essence-item-view.service';

@Component({
  selector: 'app-item',
  standalone: true,
  imports: [
    NgClass,
    NgIf,
    EssenceDetailsComponent,
    EquipmentDisplayComponent,
    PopoverComponent,
  ],
  templateUrl: './item.component.html',
})
export class ItemComponent {
  @Input() item!: ItemInstance;
  itemHovered: boolean = false;
  tooltipPosition = {};

  constructor(private readonly essenceItemView: EssenceItemViewService) {}

  get isEssence(): boolean {
    return this.item.itemBase.itemType === ItemType.Essence;
  }

  get isEquipment(): boolean {
    return this.item.itemBase.itemType === ItemType.Equipment;
  }

  itemAsEssence(item: ItemInstance) {
    return this.essenceItemView.asEssence(item.itemBase as EssenceItem);
  }

  itemAsEquipment(item: ItemInstance) {
    return item as EquipmentInstance;
  }

  get rarityClasses() {
    let rarity = Rarity.Common;
    const equipmentInstance = this.item as EquipmentInstance;

    if (equipmentInstance && equipmentInstance.rarity !== undefined) {
      rarity = equipmentInstance.rarity;
    } else {
      rarity = this.item.itemBase.rarity;
    }

    switch (rarity) {
      case Rarity.Common:
        return 'text-slate-200';
      case Rarity.Uncommon:
        return 'text-emerald-600';
      case Rarity.Rare:
        return 'text-blue-600';
      case Rarity.Epic:
        return 'text-fuchsia-600';
      case Rarity.Unique:
        return 'text-yellow-400';
      case Rarity.Legendary:
        return 'text-orange-600';
      case Rarity.Legacy:
        return 'text-rose-700';
      default:
        return 'text-light_gray';
    }
  }
}
