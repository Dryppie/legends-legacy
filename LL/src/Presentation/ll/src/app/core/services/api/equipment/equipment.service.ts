import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { Observable } from 'rxjs';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../shared/models/item';

@Injectable({
  providedIn: 'root',
})
export class EquipmentService {
  constructor(private apiService: ApiService) {}

  public getEquipment(): Observable<EquipmentSlot[]> {
    return this.apiService.get('equipment').pipe();
  }

  public equipEquipment(
    equipment: EquipmentInstance,
    slotType: EquipmentSlotType,
  ) {
    const equipmentRequestDto = {
      equipmentItemId: equipment.id,
      slotType: slotType,
    };
    return this.apiService
      .post('equipment/equip', equipmentRequestDto)
      .subscribe({
        next: () => {
          // this.characterManager.updateEquipment(equipment);
          // this.inventoryState.removeItem(equipment.id);
          // this.toastService.showToast(
          //   'Essence equipped successfully!',
          //   'success',
          //   true,
          // );
        },
        error: (error) => {
          console.error('Failed to equip essence: ', error);
        },
      });
  }

  unequipEquipment(slotType: EquipmentSlotType) {
    return this.apiService.post('equipment/unequip', slotType).subscribe({
      next: () => {
        // this.toastService.showToast(
        //   'Essence equipped successfully!',
        //   'success',
        //   true,
        // );
      },
      error: (error) => {
        console.error('Failed to equip essence: ', error);
      },
    });
  }
}
