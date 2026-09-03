import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import {
  EquipmentAccess,
  StarterEquipmentOption,
  CombatAcquisition,
  EquipmentProtectionPool,
  EquipmentProgressionRecoveryOption,
  PlainEquipmentRecoveryOption,
} from '../../../../shared/models/equipment-progression';

@Injectable({ providedIn: 'root' })
export class EquipmentProgressionService {
  private readonly api = inject(ApiService);
  access(): Observable<EquipmentAccess> {
    return this.api.get('equipmentacquisition/access');
  }
  starters(): Observable<StarterEquipmentOption[]> {
    return this.api.get('equipment/starter-options');
  }
  ordinary(): Observable<CombatAcquisition[]> {
    return this.api.get('equipmentacquisition/ordinary');
  }
  sources(): Observable<EquipmentProtectionPool[]> {
    return this.api.get('equipmentacquisition/sources');
  }
  recovery(): Observable<EquipmentProgressionRecoveryOption[]> {
    return this.api.get('equipmentacquisition/recovery');
  }
  plainRecovery(): Observable<PlainEquipmentRecoveryOption[]> {
    return this.api.get('equipmentacquisition/plain-recovery');
  }
}
