import { InventoryItem } from './inventoryItem';

export interface EssenceDefinitionDto {
  id: string;
  sourceMonsterId: string;
  name: string;
  description: string;
  rarity: string;
  tagsByCategory: Record<string, string[]>;
  attributeBonuses: EssenceAttributeBonusDto[];
  activeAbility: EssenceAbilityDto;
  passiveAbility: EssenceAbilityDto;
  evolution: EssenceEvolutionDto;
  drop: EssenceDropDto;
}

export interface EssenceAttributeBonusDto {
  attribute: string;
  modifierKind: string;
  baseValue: number;
  currentValue: number;
}

export interface EssenceAbilityDto {
  id: string;
  kind: 'Active' | 'Passive';
  name: string;
  description: string;
  cooldownSeconds: number;
  targeting: string;
  tags: string[];
  effects: EssenceEffectDto[];
}

export interface EssenceEffectDto {
  id: string;
  type: string;
  target: string;
  baseValue?: number;
  currentValue: number;
  attribute?: string | null;
  status?: string | null;
  durationSeconds?: number | null;
  scaling?: EssenceEffectScalingDto[];
  nestedEffects?: EssenceEffectDto[];
}

export interface EssenceEffectScalingDto {
  attribute: string;
  coefficient: number;
}

export interface EssenceEvolutionDto {
  id: string;
  name: string;
  description: string;
  requiredAscensionTier: number;
  requiredCatalystItemId: string;
  addsTags: string[];
}

export interface EssenceDropDto {
  baseDropChance: number;
  resonanceGainPerFailedEligibleKill: number;
  dropChanceBonusPerResonance: number;
  maxResonanceBonus: number;
}

export interface SoulArchiveDto {
  essences: PlayerEssenceDto[];
  essenceDust: number;
}

export interface PlayerEssenceDto {
  id: string;
  essenceDefinitionId: string;
  name: string;
  level: number;
  currentXp: number;
  xpRequiredForNextLevel: number;
  ascensionTier: number;
  tierLevelCap: number;
  isEvolved: boolean;
  isFavorite: boolean;
  attunedSlot?: number | null;
  canAscend: boolean;
  canEvolve: boolean;
  missingRequirements: string[];
  ascendInfo: EssenceAscendInfoDto;
  evolveInfo: EssenceEvolveInfoDto;
  currentAttributeBonuses: EssenceAttributeBonusDto[];
  activeAbility: EssenceAbilityDto;
  passiveAbility: EssenceAbilityDto;
}

export interface EssenceAscendInfoDto {
  canPerform: boolean;
  currentTier: number;
  nextTier?: number | null;
  requiredItemId?: string | null;
  requiredItemName?: string | null;
  requirements: string[];
  effects: string[];
}

export interface EssenceEvolveInfoDto {
  canPerform: boolean;
  name: string;
  description: string;
  requiredAscensionTier: number;
  requiredItemId: string;
  requiredItemName: string;
  requirements: string[];
  effects: string[];
}

export interface EssenceLoadoutsDto {
  loadouts: EssenceLoadoutDto[];
  limit: number;
  unlockedSlots: number;
}

export interface EssenceLoadoutDto {
  id: string;
  name: string;
  isActive: boolean;
  slots: EssenceLoadoutSlotDto[];
}

export interface EssenceLoadoutSlotDto {
  slotIndex: number;
  playerEssenceId?: string | null;
  essenceDefinitionId?: string | null;
  essenceName?: string | null;
}

export interface SaveEssenceLoadoutDto {
  id?: string | null;
  name: string;
  slots: SaveEssenceLoadoutSlotDto[];
}

export interface SaveEssenceLoadoutSlotDto {
  slotIndex: number;
  playerEssenceId?: string | null;
}

export interface ResponseMessageDto {
  succeeded: boolean;
  message: string;
}

export interface DismantleEssenceResultDto extends ResponseMessageDto {
  dustGained: number;
}

export interface SpendEssenceDustResultDto extends ResponseMessageDto {
  dustSpent: number;
  xpGained: number;
  levelsGained: number;
  reachedTierCap: boolean;
}

export interface EssenceMutationResponseDto extends ResponseMessageDto {
  archive: SoulArchiveDto;
  inventoryItems: InventoryItem[];
  dustGained?: number | null;
  dustSpent?: number | null;
  xpGained?: number | null;
  levelsGained?: number | null;
  reachedTierCap?: boolean | null;
}
