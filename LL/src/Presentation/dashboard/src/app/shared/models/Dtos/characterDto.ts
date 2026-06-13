import { AttributeDto } from './attributesDto';

export interface CharacterDto {
  id: string;
  name: string;
  level: number;
  experience: number;
  experienceUntilNextLevel: number;
  gold: number;
  rawAttributes?: Array<AttributeDto>;
  attributes?: Array<AttributeDto>;
}

export interface CharacterOverviewDto {
  level: number;
  baseAttributes: AttributeDto[];
  baseCombatAttributes: AttributeDto[];
  activeEssenceLoadout?: EssenceLoadoutDto | null;
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
