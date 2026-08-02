import { Component, Input } from '@angular/core';
import { InventoryItem } from '../../models/inventoryItem';
import { NgIf } from '@angular/common';
import { ModalService } from '../../../core/services/client-side/modal/modal.service';
import { ItemType } from '../../models/enums/itemType';
import { EquipmentInstance, EssenceItem } from '../../models/item';
import { ItemComponent } from '../item/item.component';
import { EssenceItemViewService } from '../../../core/services/api/essences/essence-item-view.service';

@Component({
  selector: 'app-inventory-item',
  imports: [ItemComponent, NgIf],
  templateUrl: './inventory-item.component.html',
})
export class InventoryItemComponent {
  @Input() inventoryItem!: InventoryItem;

  constructor(
    private modal: ModalService,
    private readonly essenceItemView: EssenceItemViewService,
  ) {}

  get isEquipment(): boolean {
    return (
      this.inventoryItem.itemInstance.itemBase.itemType === ItemType.Equipment
    );
  }

  get isEssence(): boolean {
    return (
      this.inventoryItem.itemInstance.itemBase.itemType === ItemType.Essence
    );
  }

  get isBlueprint(): boolean {
    const itemBase = this.inventoryItem.itemInstance.itemBase;
    return (
      itemBase.itemType === ItemType.Resource &&
      (itemBase.id.toLowerCase().startsWith('blueprint_') ||
        itemBase.name.toLowerCase().startsWith('blueprint:'))
    );
  }

  openModal(): void {
    if (this.isEquipment) {
      this.modal.toggleInventoryEquipItemModal(
        this.inventoryItem.itemInstance as EquipmentInstance,
      );
    } else if (this.isEssence) {
      const essence = this.essenceItemView.asEssence(
        this.inventoryItem.itemInstance.itemBase as EssenceItem,
      );
      this.modal.toggleEssenceModal(essence);
    } else if (
      this.isBlueprint ||
      !!this.inventoryItem.itemInstance.itemBase.selectionCrate
    ) {
      this.modal.toggleInventoryItemModal(this.inventoryItem);
    }
  }
}
