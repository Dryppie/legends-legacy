import { Injectable } from '@angular/core';
import { ApiService, VersionedMutationResult } from '../api.service';
import { Observable } from 'rxjs';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../shared/models/item';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { HttpParams } from '@angular/common/http';
import { AttributeType } from '../../../../shared/models/enums/attributeType';

export interface EquipmentChangeResponse {
  equipmentSlots: EquipmentSlot[];
  inventoryItems: InventoryItem[];
}

export interface EquipmentComparisonValue {
  attributeType: AttributeType;
  before: number;
  after: number;
  difference: number;
}

export interface EquipmentComparison {
  equipmentInstanceId: string;
  characterLevel: number;
  slotType: EquipmentSlotType;
  ratings: EquipmentComparisonValue[];
  effectiveAttributes: EquipmentComparisonValue[];
}

@Injectable({
  providedIn: 'root',
})
export class EquipmentService {
  constructor(private apiService: ApiService) {}

  public getEquipment(): Observable<EquipmentSlot[]> {
    return this.apiService.get('equipment').pipe();
  }

  public compareEquipment(
    equipmentInstanceId: string,
    slotType: EquipmentSlotType,
  ): Observable<EquipmentComparison> {
    const params = new HttpParams().set('slotType', slotType);
    return this.apiService.get(
      `equipment/comparison/${equipmentInstanceId}`,
      params,
    );
  }

  public equipEquipment(
    equipment: EquipmentInstance,
    slotType: EquipmentSlotType,
  ): Observable<VersionedMutationResult<EquipmentChangeResponse>> {
    const equipmentRequestDto = {
      equipmentItemId: equipment.id,
      slotType: slotType,
    };
    return this.apiService.postVersioned<EquipmentChangeResponse>(
      'equipment/equip',
      equipmentRequestDto,
      {
        stateSyncScopesHandledByResponse: ['equipment', 'inventory'],
      },
    );
  }

  unequipEquipment(
    slotType: EquipmentSlotType,
  ): Observable<VersionedMutationResult<EquipmentChangeResponse>> {
    return this.apiService.postVersioned<EquipmentChangeResponse>(
      'equipment/unequip',
      slotType,
      {
        stateSyncScopesHandledByResponse: ['equipment', 'inventory'],
      },
    );
  }
}
