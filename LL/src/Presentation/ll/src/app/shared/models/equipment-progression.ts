import { EquipmentType } from './enums/equipmentType';

export type StarterEquipmentKind = 'FirstWeapon' | 'ReadyForRoad';
export type EquipmentOwnership =
  | 'BoundPersonal'
  | 'UnboundPersonal'
  | 'GuildOwned';

export interface EquipmentProgression {
  modelVersion: number;
  balanceVersion: number;
  definitionId: string;
  archetypeId: string;
  rank: number;
  nativeStyleId: string | null;
  activeStyleId: string | null;
  ownership: EquipmentOwnership;
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
  baselineRecoveryEnabled: boolean;
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
      access.baselineRecoveryEnabled ||
      access.ordinaryAcquisitionEnabled)
  );
}

export interface EquipmentProgressionItem {
  id: string;
  definitionId: string;
  nativeStyleId: string | null;
  displayName: string;
  tier: number;
  rank: number;
  balanceVersion: number;
  rarity: string;
  activeStyleId: string | null;
  equipmentSetId: string | null;
  ownership: EquipmentOwnership;
  stats: Record<string, number>;
}

export interface CombatAcquisition {
  poolId: string;
  rulesVersion: string;
  regionName: string;
  equipmentTier: number;
  hasEnteredRegion: boolean;
  selectedDefinitionId: string | null;
  plainVictories: number;
  requiredPlainVictories: number;
  selectedSigilFamilyId: string | null;
  sigilVictories: number;
  requiredSigilVictories: number;
  discoveryChance: number;
  targets: StarterEquipmentOption[];
  sigils: {
    familyId: string;
    itemBaseId: string;
    canSelect: boolean;
    unavailableReason: string | null;
  }[];
}

export interface EquipmentProtectionPool {
  pool: {
    id: string;
    dungeonId: string;
    familyId: string;
    difficulty: number;
    equipmentTier: number;
    minimumLevel: number;
    requiredQuestId: string;
    matchingChance: number;
    guaranteeCompletions: number;
  };
  selectedDefinitionId: string | null;
  progress: number;
  firstClearGuaranteeAvailable: boolean;
  canSelect: boolean;
  missingRequirements: string[];
  targets: EquipmentProgressionItem[];
}

export interface PlainEquipmentRecoveryOption {
  definitionId: string;
  tier: number;
  name: string;
  entitled: number;
  owned: number;
  missing: number;
}

export interface EquipmentProgressionRecoveryOption {
  kind: StarterEquipmentKind;
  definitionId: string;
  name: string;
  entitled: number;
  owned: number;
  missing: number;
}
