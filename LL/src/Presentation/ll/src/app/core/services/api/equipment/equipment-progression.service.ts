import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import {
  EquipmentAccess,
  StarterEquipmentOption,
  ForgeRequest,
  ForgeQuote,
  ForgeStyle,
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
  styles(itemInstanceId: string): Observable<ForgeStyle[]> {
    return this.api.get(
      'forge/styles',
      new HttpParams().set('itemInstanceId', itemInstanceId),
    );
  }
  preview(request: ForgeRequest): Observable<ForgeQuote> {
    return this.api.post('forge/preview', request, {
      forceStateSyncRefresh: false,
    });
  }
  mutate<T>(path: string, body: object): Observable<T> {
    // These responses are receipts/quotes, not complete inventory or character snapshots.
    // Let the normal response interceptor refresh every invalidated scope.
    return this.api.post(path, body);
  }
}
