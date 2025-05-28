import { EssenceSlot } from '../essenceSlot';
import { AttributeDto } from './attributesDto';

export interface CharacterDto {
  id: string;
  name: string;
  level: number;
  experience: number;
  experienceUntilNextLevel: number;
  cinders: number;
  soulstones: number;
  arenaRating: number;
}

export interface CharacterOverviewDto {
  level: number;
  baseAttributes: AttributeDto[];
  baseCombatAttributes: AttributeDto[];
  essenceSlots: EssenceSlot[];
}
