import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { Observable } from 'rxjs';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../shared/models/item';
import { InventoryItem } from '../../../../shared/models/inventoryItem';

export interface EquipmentChangeResponse {
  equipmentSlots: EquipmentSlot[];
  inventoryItems: InventoryItem[];
}

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
  ): Observable<EquipmentChangeResponse> {
    const equipmentRequestDto = {
      equipmentItemId: equipment.id,
      slotType: slotType,
    };
    return this.apiService.post('equipment/equip', equipmentRequestDto);
  }

  unequipEquipment(
    slotType: EquipmentSlotType,
  ): Observable<EquipmentChangeResponse> {
    return this.apiService.post('equipment/unequip', slotType);
  }
}
