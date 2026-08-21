import { Component, Input } from '@angular/core';
import {
  Equipment,
  EquipmentInstance,
  EssenceItem,
  ItemInstance,
} from '../../models/item';
import { NgClass, NgIf } from '@angular/common';
import { Rarity } from '../../models/enums/rarity';
import { EssenceDetailsComponent } from '../essences/essence-details/essence-details.component';
import { EquipmentDisplayComponent } from '../equipment/equipment-display/equipment-display.component';
import { ItemType } from '../../models/enums/itemType';
import { EquipmentType } from '../../models/enums/equipmentType';
import { PopoverComponent } from '../custom-components/popover/popover.component';
import { EssenceItemViewService } from '../../../core/services/api/essences/essence-item-view.service';
import { InventoryStateService } from '../../../core/services/api/inventory/inventory-state.service';
import { EquipmentStateService } from '../../../core/services/api/equipment/equipment-state.service';
import {
  EquippedComparison,
  findEquippedComparisons,
} from '../../utils/equipment/equipment.utils';
import { BlueprintAttributeSummaryComponent } from '../blueprint-attribute-summary/blueprint-attribute-summary.component';

@Component({
  selector: 'app-item',
  imports: [
    NgClass,
    NgIf,
    EssenceDetailsComponent,
    EquipmentDisplayComponent,
    PopoverComponent,
    BlueprintAttributeSummaryComponent,
  ],
  templateUrl: './item.component.html',
})
export class ItemComponent {
  @Input() item!: ItemInstance;
  @Input() popoverTouchDisabled = false;
  itemHovered: boolean = false;
  tooltipPosition = {};

  constructor(
    private readonly essenceItemView: EssenceItemViewService,
    private readonly inventoryState: InventoryStateService,
    private readonly equipmentState: EquipmentStateService,
  ) {}

  get isEssence(): boolean {
    return this.item.itemBase.itemType === ItemType.Essence;
  }

  get isEquipment(): boolean {
    return this.item.itemBase.itemType === ItemType.Equipment;
  }

  get borrowedFromGuildName(): string | null {
    if (!this.isEquipment) return null;
    const equipment = this.item as EquipmentInstance;
    return equipment.isGuildBorrowed
      ? equipment.borrowedFromGuildName || 'guild vault'
      : null;
  }

  get isGenericItem(): boolean {
    return !this.isEssence && !this.isEquipment;
  }

  get displayName(): string {
    if (this.item.displayName) {
      return this.item.displayName;
    }

    if (this.isTool) {
      return this.getToolDisplayName(this.item.itemBase.name, this.rarity);
    }

    return this.item.itemBase.name;
  }

  get isFavorite(): boolean {
    return this.inventoryState.isFavorite(this.item.id);
  }

  get isTool(): boolean {
    const base = this.item.itemBase as Equipment;
    const instance = this.item as EquipmentInstance;

    return (
      instance.equipmentBase?.equipmentType === EquipmentType.Tool ||
      base.equipmentType === EquipmentType.Tool
    );
  }

  get rewardSource(): string | null {
    return typeof this.item.source === 'string' ? this.item.source : null;
  }

  get rewardCategory(): string | null {
    return typeof this.item.category === 'string' ? this.item.category : null;
  }

  itemAsEssence(item: ItemInstance) {
    return this.essenceItemView.asEssence(item.itemBase as EssenceItem);
  }

  itemAsEquipment(item: ItemInstance): Equipment | EquipmentInstance {
    return 'equipmentBase' in item
      ? (item as EquipmentInstance)
      : (item.itemBase as Equipment);
  }

  get ownedQuantity(): number {
    return this.inventoryState
      .items()
      .filter(
        (inventoryItem) =>
          inventoryItem.itemInstance.itemBase.id === this.item.itemBase.id,
      )
      .reduce((total, inventoryItem) => total + inventoryItem.quantity, 0);
  }

  get equippedComparisons(): EquippedComparison[] {
    if (!this.isEquipment) return [];

    return findEquippedComparisons(
      this.itemAsEquipment(this.item),
      this.equipmentState.equipmentSlots(),
    );
  }

  get rarityClasses() {
    switch (this.rarity) {
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

  private get rarity(): Rarity {
    const equipmentInstance = this.item as EquipmentInstance;

    if (equipmentInstance && equipmentInstance.rarity !== undefined) {
      return equipmentInstance.rarity;
    }

    return this.item.itemBase.rarity;
  }

  private getToolDisplayName(baseName: string, rarity: Rarity): string {
    switch (rarity) {
      case Rarity.Common:
        return `Plain ${baseName}`;
      case Rarity.Uncommon:
        return `Sturdy ${baseName}`;
      case Rarity.Rare:
        return `Proven ${baseName}`;
      case Rarity.Epic:
        return `Exquisite ${baseName}`;
      case Rarity.Unique:
        return `Fabled ${baseName}`;
      case Rarity.Legendary:
        return `Mythic ${baseName}`;
      case Rarity.Legacy:
        return `Eternal ${baseName}`;
      default:
        return baseName;
    }
  }
}
