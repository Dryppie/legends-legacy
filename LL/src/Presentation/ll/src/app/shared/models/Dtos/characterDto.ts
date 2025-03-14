import { EssenceSlot } from '../essenceSlot';
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
  essenceSlots: EssenceSlot[];
}
