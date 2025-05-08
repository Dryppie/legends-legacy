import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { Observable } from 'rxjs';
import { EquipmentSlot } from '../../../../shared/models/Dtos/equipmentSlot';
import { Equipment } from '../../../../shared/models/item';

@Injectable({
  providedIn: 'root',
})
export class EquipmentService {
  constructor(private apiService: ApiService) {}

  public getEquipment(): Observable<EquipmentSlot[]> {
    return this.apiService.get('equipment');
  }

  public equipEquipment(equipment: Equipment) {
    return this.apiService.post('equipment/equip', equipment.id).subscribe({
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
