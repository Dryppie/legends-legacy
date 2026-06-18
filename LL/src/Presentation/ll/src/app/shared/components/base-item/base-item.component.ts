import { NgClass, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { EssenceItem, Equipment, ItemBase } from '../../models/item';
import { ItemType } from '../../models/enums/itemType';
import { Rarity } from '../../models/enums/rarity';
import { EssenceItemViewService } from '../../../core/services/api/essences/essence-item-view.service';
import { PopoverComponent } from '../custom-components/popover/popover.component';
import { EssenceDetailsComponent } from '../essences/essence-details/essence-details.component';
import { EquipmentDisplayComponent } from '../equipment/equipment-display/equipment-display.component';

@Component({
  selector: 'app-base-item',
  standalone: true,
  imports: [
    NgClass,
    NgIf,
    PopoverComponent,
    EssenceDetailsComponent,
    EquipmentDisplayComponent,
  ],
  templateUrl: './base-item.component.html',
})
export class BaseItemComponent {
  @Input({ required: true }) item!: ItemBase;
  @Input() useBaseName = false;

  constructor(private readonly essenceItemView: EssenceItemViewService) {}

  get isEssence(): boolean {
    return this.item.itemType === ItemType.Essence;
  }

  get isEquipment(): boolean {
    return this.item.itemType === ItemType.Equipment;
  }

  get isGenericItem(): boolean {
    return !this.isEssence && !this.isEquipment;
  }

  itemAsEssence(item: ItemBase) {
    return this.essenceItemView.asEssence(item as EssenceItem);
  }

  itemAsEquipment(item: ItemBase): Equipment {
    return item as Equipment;
  }

  get rarityClasses() {
    if (this.useBaseName) {
      return 'text-zinc-100';
    }

    switch (this.item.rarity) {
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
