import { Component, Input, OnInit } from '@angular/core';
import { InventoryItem } from '../../models/inventoryItem';
import { ItemComponent } from '../item/item.component';
import { NgIf } from '@angular/common';
import { ModalService } from '../../../core/services/client-side/modal/modal.service';
import { ItemType } from '../../models/enums/itemType';
import { Equipment, EquipmentInstance, EssenceItem } from '../../models/item';

@Component({
  selector: 'app-inventory-item',
  standalone: true,
  imports: [ItemComponent, NgIf],
  templateUrl: './inventory-item.component.html',
  styleUrl: './inventory-item.component.css',
})
export class InventoryItemComponent implements OnInit {
  @Input() inventoryItem!: InventoryItem;
  equipmentIcon = '';
  isEquipment = false;
  isEssence = false;

  constructor(private modalService: ModalService) {}

  ngOnInit(): void {
    this.setIsEquipment();
    this.setIsEssence();
    if (this.isEquipment) {
      this.equipmentIcon = (
        this.inventoryItem.itemInstance.itemBase as Equipment
      ).equipmentType.toLocaleLowerCase();
    }
  }

  openModal() {
    if (this.isEquipment) this.openEquipItemModal();
    else if (this.isEssence) this.openEssenceModal();
  }

  setIsEquipment() {
    this.isEquipment =
      this.inventoryItem.itemInstance.itemBase.itemType === ItemType.Equipment;
  }

  openEquipItemModal() {
    this.modalService.toggleInventoryEquipItemModal(
      this.inventoryItem.itemInstance as EquipmentInstance,
    );
  }

  setIsEssence() {
    this.isEssence =
      this.inventoryItem.itemInstance.itemBase.itemType === ItemType.Essence;
  }

  openEssenceModal() {
    this.modalService.toggleEssenceModal(
      (this.inventoryItem.itemInstance.itemBase as EssenceItem).essence,
    ); // Pass the essence from the Item to display all necessary info
  }
}
