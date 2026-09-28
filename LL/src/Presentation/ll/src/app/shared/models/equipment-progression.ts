import { EquipmentType } from './enums/equipmentType';
import { ItemQuality } from './enums/itemQuality';

export type StarterEquipmentKind = 'FirstWeapon' | 'ReadyForRoad';
export type EquipmentOwnership =
  | 'BoundPersonal'
  | 'UnboundPersonal'
  | 'GuildOwned';

export interface EquipmentProgression {
  statVersion?: number;
  allocation?: EquipmentBudgetAllocation | null;
  modelVersion: number;
  balanceVersion: number;
  definitionId: string;
  archetypeId: string;
  rank: number;
  quality: ItemQuality;
  attributeRollMultiplier: number;
  nativeStyleId: string | null;
  activeStyleId: string | null;
  ownership: EquipmentOwnership;
}

export interface EquipmentBudgetAllocation {
  statVersion: number;
  core: number;
  specialization: number;
  styleStats: number;
  reservedIdentity: number;
  total: number;
  specializationId: string;
}

export interface StarterEquipmentOption {
  definitionId: string;
  name: string;
  equipmentType: EquipmentType;
  stats: Record<string, number>;
}

export interface StarterEquipmentGrant {
  kind: StarterEquipmentKind;
  grantedAtUtc: string;
  equipmentIds: string[];
  definitionIds: string[];
}

export interface EquipmentAccess {
  starterAcquisitionEnabled: boolean;
  protectedAcquisitionEnabled: boolean;
  ordinaryAcquisitionEnabled: boolean;
  starters: {
    kind: StarterEquipmentKind;
    canClaim: boolean;
    unavailableReason: string | null;
    grant: StarterEquipmentGrant | null;
  }[];
}

export function hasEquipmentProgressionAccess(
  access: EquipmentAccess | null,
): boolean {
  return (
    !!access &&
    (access.starterAcquisitionEnabled ||
      access.protectedAcquisitionEnabled ||
      access.ordinaryAcquisitionEnabled)
  );
}

export interface EquipmentProgressionItem {
  allocation?: EquipmentBudgetAllocation | null;
  id: string;
  definitionId: string;
  nativeStyleId: string | null;
  displayName: string;
  tier: number;
  rank: number;
  balanceVersion: number;
  rarity: string;
  quality: ItemQuality;
  attributeRollMultiplier: number;
  activeStyleId: string | null;
  equipmentSetId: string | null;
  ownership: EquipmentOwnership;
  stats: Record<string, number>;
}
