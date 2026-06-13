import { AttributeDto } from './attributesDto';
import { EssenceLoadoutDto } from '../essence-system';

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
  powerScore: number;
  baseAttributes: AttributeDto[];
  baseCombatAttributes: AttributeDto[];
  activeEssenceLoadout?: EssenceLoadoutDto | null;
}
