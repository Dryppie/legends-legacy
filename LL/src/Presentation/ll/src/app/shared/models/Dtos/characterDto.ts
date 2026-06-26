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
  fateEcho: number;
  sigilFragments: number;
  ascensionStoneFragments: number;
  arenaRating: number;
  equippedTitle?: EquippedTitleDto | null;
}

export interface CharacterOverviewDto {
  id: string;
  level: number;
  combatRating: number;
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
