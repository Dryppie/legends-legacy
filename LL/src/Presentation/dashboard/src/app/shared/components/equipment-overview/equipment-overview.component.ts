import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { EquipmentService } from '../../../core/services/api/equipment/equipment.service';
import { EquipmentSlot, EquipmentType } from '../../models/Dtos/equipmentSlot';

@Component({
  selector: 'app-equipment-overview',
  standalone: true,
  imports: [NgFor, NgIf],
  templateUrl: './equipment-overview.component.html',
  styleUrl: './equipment-overview.component.css',
})
export class EquipmentOverviewComponent implements OnInit {
  handleSlotClick(equipmentSlot: EquipmentSlot) {
    throw new Error('Method not implemented.');
  }
  constructor(private equipmentService: EquipmentService) {}
  slots = this.setInitialEquipmentSlots();

  ngOnInit(): void {
    this.loadEquipment();
  }

  private loadEquipment(): void {
    this.equipmentService
      .getEquipment()
      .subscribe((equipmentList: EquipmentSlot[]) => {
        equipmentList.forEach((equipmentSlot) => {
          // Here, assume equipmentItem has something like: { slotName: 'Head', ... }
          const matchingSlot = this.slots.find(
            (s) => s.equipmentType === equipmentSlot.equipmentType,
          );
          if (matchingSlot) {
            matchingSlot.equipmentInstance = equipmentSlot.equipmentInstance;
          }
        });
      });
  }

  private setInitialEquipmentSlots(): EquipmentSlot[] {
    return [
      {
        id: '',
        iconPath: 'empty_helmet',
        equipmentType: EquipmentType.Head,
      },
      {
        id: '',
        iconPath: 'empty_cloak',
        equipmentType: EquipmentType.Relic,
      },
      {
        id: '',
        iconPath: 'empty_armor',
        equipmentType: EquipmentType.Chest,
      },
      {
        id: '',
        iconPath: 'empty_necklace',
        equipmentType: EquipmentType.Necklace,
      },
      {
        id: '',
        iconPath: 'empty_legs',
        equipmentType: EquipmentType.Legs,
      },
      {
        id: '',
        iconPath: 'empty_ring',
        equipmentType: EquipmentType.Ring,
      },
      {
        id: '',
        iconPath: 'empty_mainhand',
        equipmentType: EquipmentType.OneHanded,
      },
      {
        id: '',
        iconPath: 'empty_offhand',
        equipmentType: EquipmentType.OffHand,
      },
    ];
  }
}
