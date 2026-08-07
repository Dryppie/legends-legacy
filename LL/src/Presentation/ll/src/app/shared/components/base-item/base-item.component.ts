import { NgClass, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import {
  EssenceItem,
  Equipment,
  EquipmentInstance,
  ItemBase,
} from '../../models/item';
import { ItemType } from '../../models/enums/itemType';
import { Rarity } from '../../models/enums/rarity';
import { EssenceItemViewService } from '../../../core/services/api/essences/essence-item-view.service';
import { PopoverComponent } from '../custom-components/popover/popover.component';
import { EssenceDetailsComponent } from '../essences/essence-details/essence-details.component';
import { EquipmentDisplayComponent } from '../equipment/equipment-display/equipment-display.component';
import { InventoryStateService } from '../../../core/services/api/inventory/inventory-state.service';
import { EquipmentStateService } from '../../../core/services/api/equipment/equipment-state.service';
import { findEquippedComparison } from '../../utils/equipment/equipment.utils';

@Component({
  selector: 'app-base-item',
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

  constructor(
    private readonly essenceItemView: EssenceItemViewService,
    private readonly inventoryState: InventoryStateService,
    private readonly equipmentState: EquipmentStateService,
  ) {}

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

  get ownedQuantity(): number {
    return this.inventoryState
      .items()
      .filter(
        (inventoryItem) =>
          inventoryItem.itemInstance.itemBase.id === this.item.id,
      )
      .reduce((total, inventoryItem) => total + inventoryItem.quantity, 0);
  }

  get equippedComparison(): EquipmentInstance | null {
    if (!this.isEquipment) return null;

    return findEquippedComparison(
      this.itemAsEquipment(this.item),
      this.equipmentState.equipmentSlots(),
    );
  }

  get rarityClasses() {
    if (this.useBaseName) {
      return 'text-white';
    }

    switch (this.item.rarity) {
      case Rarity.Common:
        return 'll-rarity-common';
      case Rarity.Uncommon:
        return 'll-rarity-uncommon';
      case Rarity.Rare:
        return 'll-rarity-rare';
      case Rarity.Epic:
        return 'll-rarity-epic';
      case Rarity.Unique:
        return 'll-rarity-unique';
      case Rarity.Legendary:
        return 'll-rarity-legendary';
      case Rarity.Legacy:
        return 'll-rarity-legacy';
      default:
        return 'll-text-muted';
    }
  }
}
