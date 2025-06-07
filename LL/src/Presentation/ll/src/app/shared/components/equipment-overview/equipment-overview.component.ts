import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { EquipmentService } from '../../../core/services/api/equipment/equipment.service';
import { EquipmentSlot, EquipmentType } from '../../models/Dtos/equipmentSlot';
import { ModalService } from '../../../core/services/client-side/modal/modal.service';
import { CharacterManagerService } from '../../../core/services/client-side/character-manager/character-manager.service';
import { ItemComponent } from '../item/item.component';

@Component({
  selector: 'app-equipment-overview',
  standalone: true,
  imports: [NgFor, NgIf, ItemComponent],
  templateUrl: './equipment-overview.component.html',
})
export class EquipmentOverviewComponent implements OnInit {
  constructor(
    private equipmentService: EquipmentService,
    private modalService: ModalService,
    private characterManager: CharacterManagerService,
  ) {}
  slots = this.setInitialEquipmentSlots();

  ngOnInit(): void {
    this.characterManager.equipment$.subscribe((equipmentList) => {
      if (!equipmentList) return;
      equipmentList.forEach((equipmentSlot) => {
        const matchingSlot = this.slots.find(
          (s) => s.equipmentType === equipmentSlot.equipmentType,
        );
        if (matchingSlot) {
          matchingSlot.equipmentInstance = equipmentSlot.equipmentInstance;
        }
      });
    });
    this.loadEquipment();
  }

  private loadEquipment(): void {
    this.equipmentService.getEquipment().subscribe();
  }

  handleSlotClick(equipmentSlot: EquipmentSlot) {
    // const inventory = this.characterManager.getInventory();
    // if (!inventory) return;
    // const matchingItems = inventory.inventoryItems
    //   .map((ii) => ii.itemInstance)
    //   .filter(
    //     (item) =>
    //       (item.itemBase as Equipment).equipmentType ===
    //       equipmentSlot.equipmentType,
    //   ) as EquipmentInstance[];
    // if (!matchingItems) {
    //   const filler: EquipmentInstance {

    //   }
    //   matchingItems.push();
    // }
    // You can pass the filtered items to your modal, if needed
    this.modalService.toggleOverviewEquipItemModal(equipmentSlot.equipmentType);
  }

  private setInitialEquipmentSlots(): EquipmentSlot[] {
    return [
      {
        id: '',
        iconPath: 'empty_head',
        equipmentType: EquipmentType.Head,
      },
      {
        id: '',
        iconPath: 'empty_chest',
        equipmentType: EquipmentType.Chest,
      },
      {
        id: '',
        iconPath: 'empty_legs',
        equipmentType: EquipmentType.Legs,
      },
      {
        id: '',
        iconPath: 'empty_relic',
        equipmentType: EquipmentType.Relic,
      },
      {
        id: '',
        iconPath: 'empty_necklace',
        equipmentType: EquipmentType.Necklace,
      },
      {
        id: '',
        iconPath: 'empty_ring',
        equipmentType: EquipmentType.Ring,
      },
      {
        id: '',
        iconPath: 'empty_mainhand',
        equipmentType: EquipmentType.MainHand,
      },
      {
        id: '',
        iconPath: 'empty_offhand',
        equipmentType: EquipmentType.OffHand,
      },
    ];
  }
}
