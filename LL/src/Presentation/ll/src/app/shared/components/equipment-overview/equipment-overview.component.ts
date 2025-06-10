import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, computed, OnInit } from '@angular/core';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../models/Dtos/equipment-slots/equipmentSlot';
import { ModalService } from '../../../core/services/client-side/modal/modal.service';
import { ItemComponent } from '../item/item.component';
import { EquipmentStateService } from '../../../core/services/api/equipment/equipment-state.service';
import { EquipmentInstance } from '../../models/item';
import { EquipmentType } from '../../models/enums/equipmentType';

@Component({
  selector: 'app-equipment-overview',
  standalone: true,
  imports: [NgFor, NgIf, NgClass, ItemComponent],
  templateUrl: './equipment-overview.component.html',
})
export class EquipmentOverviewComponent implements OnInit {
  isGhost(slot: EquipmentSlot): boolean {
    return (
      slot.equipmentSlotType === EquipmentSlotType.OffHand &&
      slot.equipmentInstance?.itemBase.equipmentType === EquipmentType.TwoHanded
    );
  }
  constructor(
    private modalService: ModalService,
    private readonly equipmentState: EquipmentStateService,
  ) {}
  private readonly baseSlots = this.setInitialEquipmentSlots();

  slots = computed(() => {
    const stateSlots = this.equipmentState.equipmentSlots();
    return this.baseSlots.map((slot) => {
      const live = stateSlots.find(
        (s) => s.equipmentSlotType === slot.equipmentSlotType,
      );
      return {
        ...slot,
        ...live,
        iconPath: slot.iconPath, // ensure custom iconPath is preserved
      };
    });
  });

  ngOnInit(): void {
    this.equipmentState.load();
  }

  handleSlotClick(equipmentSlot: EquipmentSlot) {
    this.modalService.toggleOverviewEquipItemModal(
      equipmentSlot.equipmentSlotType,
    );
  }

  private setInitialEquipmentSlots(): EquipmentSlot[] {
    return [
      {
        id: '',
        iconPath: 'empty_head',
        equipmentSlotType: EquipmentSlotType.Head,
      },
      {
        id: '',
        iconPath: 'empty_chest',
        equipmentSlotType: EquipmentSlotType.Chest,
      },
      {
        id: '',
        iconPath: 'empty_legs',
        equipmentSlotType: EquipmentSlotType.Legs,
      },
      {
        id: '',
        iconPath: 'empty_relic',
        equipmentSlotType: EquipmentSlotType.Relic,
      },
      {
        id: '',
        iconPath: 'empty_necklace',
        equipmentSlotType: EquipmentSlotType.Necklace,
      },
      {
        id: '',
        iconPath: 'empty_ring',
        equipmentSlotType: EquipmentSlotType.Ring,
      },
      {
        id: '',
        iconPath: 'empty_mainhand',
        equipmentSlotType: EquipmentSlotType.MainHand,
      },
      {
        id: '',
        iconPath: 'empty_offhand',
        equipmentSlotType: EquipmentSlotType.OffHand,
      },
    ];
  }
}
