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
import { EquipmentProgressionItem } from '../../../../shared/models/equipment-progression';

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

export type EquipmentUpgradeOperationKind =
  | 'Reinforce'
  | 'Dismantle'
  | 'ApplyVariant';

export interface EquipmentBlueprintOption {
  styleId: string;
  name: string;
  itemId: string;
  held: number;
  isCurrent: boolean;
  sources: {
    name: string;
    region: number;
    completionsUntilGuaranteed: number;
  }[];
}

export interface EquipmentUpgradeRequest {
  kind: EquipmentUpgradeOperationKind;
  itemInstanceId: string;
  allowFavoriteDismantle: boolean;
  blueprintStyleId?: string;
}

export interface EquipmentUpgradeQuote {
  operationId: string;
  request: EquipmentUpgradeRequest;
  token: string;
  expiresAtUtc: string;
  canExecute: boolean;
  unavailableReason: string | null;
  before: EquipmentProgressionItem | null;
  after: EquipmentProgressionItem | null;
  partsCost: number;
  cinderCost: number;
  partsReturned: number;
  availableParts: number;
  availableCinders: number;
  itemVersion: number;
  priceVersion: number;
  blueprintItemId?: string | null;
  availableBlueprints?: number;
}

export interface EquipmentUpgradeOutcome {
  operationId: string;
  kind: EquipmentUpgradeOperationKind;
  itemInstanceId: string;
  before: EquipmentProgressionItem | null;
  after: EquipmentProgressionItem | null;
  partsSpent: number;
  cindersSpent: number;
  partsReturned: number;
  occurredAtUtc: string;
}

export interface EquipmentUpgradeMutation {
  outcome: EquipmentUpgradeOutcome | null;
  freshQuote: EquipmentUpgradeQuote | null;
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

  public previewUpgrade(
    itemInstanceId: string,
    kind: EquipmentUpgradeOperationKind,
    allowFavoriteDismantle = false,
    blueprintStyleId?: string,
  ): Observable<EquipmentUpgradeQuote> {
    return this.apiService.post('equipment/upgrade/preview', {
      kind,
      itemInstanceId,
      allowFavoriteDismantle,
      ...(blueprintStyleId ? { blueprintStyleId } : {}),
    });
  }

  public reinforce(
    quote: EquipmentUpgradeQuote,
  ): Observable<EquipmentUpgradeMutation> {
    return this.apiService.post('equipment/upgrade/reinforce', {
      operationId: quote.operationId,
      itemInstanceId: quote.request.itemInstanceId,
      quoteToken: quote.token,
    });
  }

  public getBlueprints(
    itemInstanceId: string,
  ): Observable<EquipmentBlueprintOption[]> {
    return this.apiService.get(`equipment/blueprints/${itemInstanceId}`);
  }

  public applyVariant(
    quote: EquipmentUpgradeQuote,
  ): Observable<EquipmentUpgradeMutation> {
    return this.apiService.post('equipment/upgrade/variant', {
      operationId: quote.operationId,
      itemInstanceId: quote.request.itemInstanceId,
      blueprintStyleId: quote.request.blueprintStyleId,
      quoteToken: quote.token,
    });
  }

  public dismantle(
    quote: EquipmentUpgradeQuote,
  ): Observable<EquipmentUpgradeMutation> {
    return this.apiService.post('equipment/upgrade/dismantle', {
      operationId: quote.operationId,
      itemInstanceId: quote.request.itemInstanceId,
      allowFavoriteDismantle: quote.request.allowFavoriteDismantle,
      quoteToken: quote.token,
    });
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
