import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { Observable, tap } from 'rxjs';
import {
  EquipmentSlot,
  EquipmentType,
} from '../../../../shared/models/Dtos/equipmentSlot';
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
        this.characterManager.updateEquipment(equipment);
        this.inventoryState.removeItem(equipment.id);
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

  unequipEquipment(equipmentType: EquipmentType) {
    return this.apiService.post('equipment/unequip', equipmentType).subscribe({
      next: () => {
        const equipment = this.characterManager
          .getEquipment()
          .find((e) => e.equipmentType === equipmentType);
        if (!equipment || !equipment.equipmentInstance) return;

        const inventoryItem: InventoryItem = {
          id: '',
          itemInstance: equipment.equipmentInstance,
          quantity: 1,
        };

        this.inventoryState.add(inventoryItem);
        this.characterManager.unequipEquipment(equipmentType);
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
