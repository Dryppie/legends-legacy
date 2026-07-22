import { AttributeDto } from './attributesDto';
import { EssenceLoadoutDto } from '../essence-system';
import { OverallPowerRating } from './powerRating';

export interface CharacterDto {
  id: string;
  name: string;
  level: number;
  experience: number;
  experienceUntilNextLevel: number;
  cinders: number;
  soulstones: number;
  fateEcho: number;
  sigilFragments: number;
  guildFavor: number;
  guildHonors: number;
  arenaRating: number;
  equippedTitle?: EquippedTitleDto | null;
}

export interface CharacterOverviewDto {
  id: string;
  name: string;
  level: number;
  power?: OverallPowerRating | null;
  baseAttributes: AttributeDto[];
  baseCombatAttributes: AttributeDto[];
  activeEssenceLoadout?: EssenceLoadoutDto | null;
  equippedTitle?: EquippedTitleDto | null;
}

export interface EquippedTitleDto {
  key: string;
  name: string;
  displayPosition: 'Prefix' | 'Suffix';
  displayName: string;
}
