import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { Observable, tap } from 'rxjs';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../shared/models/item';
import { CharacterManagerService } from '../../client-side/character-manager/character-manager.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { InventoryItem } from '../../../../shared/models/inventoryItem';

@Injectable({
  providedIn: 'root',
})
export class EquipmentService {
  constructor(
    private apiService: ApiService,
    private characterManager: CharacterManagerService,
    private inventoryState: InventoryStateService,
  ) {}

  public getEquipment(): Observable<EquipmentSlot[]> {
    return this.apiService
      .get('equipment')
      .pipe(tap((equipment) => this.characterManager.setEquipment(equipment)));
  }

  public equipEquipment(equipment: EquipmentInstance) {
    return this.apiService.post('equipment/equip', equipment.id).subscribe({
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
