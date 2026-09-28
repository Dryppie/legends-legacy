import { Injectable } from '@angular/core';
import { ApiService, VersionedMutationResult } from '../api.service';
import { Observable } from 'rxjs';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../shared/models/item';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { AttributeType } from '../../../../shared/models/enums/attributeType';
import { EquipmentProgressionItem } from '../../../../shared/models/equipment-progression';
import { EssenceCombatActivity } from '../../../../shared/models/essence-system';

export interface EquipmentChangeResponse {
  equipmentSlots: EquipmentSlot[];
  inventoryItems: InventoryItem[];
}

export interface MigratedSpecializationChoice {
  migrationId: string;
  options: EquipmentProgressionItem[];
}

export interface EquipmentComparisonValue {
  attributeType: AttributeType;
  before: number;
  after: number;
  difference: number;
}

export interface EquipmentComparison {
  activity?: EssenceCombatActivity | 'None';
  doctrine?: string | null;
  essenceIds?: string[];
  metrics?: { id: string; label: string; before: number; after: number; unit: string; assumption: string }[];
  setChanges?: { setId: string; name: string; bonusId: string; description: string; activeBefore: boolean; activeAfter: boolean }[];
  breakdown?: { attributeType: AttributeType; base: number; withEquipment: number; withSetsAndLoadout: number; effective: number; unusedAtCap: number }[];
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
  requiredBlueprints: number;
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
}

@Injectable({
  providedIn: 'root',
})
export class EquipmentService {
  constructor(private apiService: ApiService) {}

  getMigratedSpecializationChoice(itemId: string): Observable<MigratedSpecializationChoice | null> {
    return this.apiService.get(`equipment/migration/choice/${itemId}`);
  }

  chooseMigratedSpecialization(migrationId: string, operationId: string, definitionId: string): Observable<unknown> {
    return this.apiService.post('equipment/migration/specialization', { migrationId, operationId, definitionId });
  }

  public getLinkedEquipment(id: string): Observable<EquipmentInstance> {
    return this.apiService.get(`equipment/linked/${id}`);
  }

  public getEquipment(): Observable<EquipmentSlot[]> {
    return this.apiService.get('equipment').pipe();
  }

  public compareEquipment(
    equipmentInstanceId: string,
    slotType: EquipmentSlotType,
    activity: EssenceCombatActivity | 'None' = 'None',
  ): Observable<EquipmentComparison> {
    return this.apiService.post('equipment/compare/observe', {
      requestId: crypto.randomUUID(), equipmentInstanceId, slotType, activity,
    });
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
    });
  }

  public dismantle(
    quote: EquipmentUpgradeQuote,
  ): Observable<EquipmentUpgradeMutation> {
    return this.apiService.post('equipment/upgrade/dismantle', {
      operationId: quote.operationId,
      itemInstanceId: quote.request.itemInstanceId,
      allowFavoriteDismantle: quote.request.allowFavoriteDismantle,
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
