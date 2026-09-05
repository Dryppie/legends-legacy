import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import { EquipmentAccess } from '../../../../shared/models/equipment-progression';

@Injectable({ providedIn: 'root' })
export class EquipmentProgressionService {
  private readonly api = inject(ApiService);
  access(): Observable<EquipmentAccess> {
    return this.api.get('equipmentacquisition/access');
  }
}
