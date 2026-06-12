export interface EssenceCatalogDto {
  essences: EssenceDefinitionDto[];
  tagsByCategory: Record<string, string[]>;
}

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
  currentAttributeBonuses: EssenceAttributeBonusDto[];
  activeAbility: EssenceAbilityDto;
  passiveAbility: EssenceAbilityDto;
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
