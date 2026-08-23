import { AttributeDto } from './attributesDto';
import { EssenceLoadoutDto } from '../essence-system';
import { OverallPowerRating } from './powerRating';
import { ProfessionType } from './characterProfession';

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
  arenaRating: number;
  equippedTitle?: EquippedTitleDto | null;
}

export interface CharacterOverviewDto {
  id: string;
  name: string;
  level: number;
  experience: number;
  experienceUntilNextLevel: number;
  craftingLevel: number;
  craftingExperience: number;
  craftingExperienceUntilNextLevel: number;
  gatheringProfessions: GatheringProfessionOverviewDto[];
  power?: OverallPowerRating | null;
  baseAttributes: AttributeDto[];
  baseCombatAttributes: AttributeDto[];
  equipmentRatings?: AttributeDto[];
  essenceLoadout?: EssenceLoadoutDto | null;
  equippedTitle?: EquippedTitleDto | null;
  guild?: CharacterGuildDto | null;
  isOnline: boolean;
  lastSeenAt?: string | null;
}

export interface GatheringProfessionOverviewDto {
  professionType: ProfessionType;
  level: number;
  experience: number;
  experienceUntilNextLevel: number;
}

export interface CharacterGuildDto {
  id: string;
  name: string;
  tag: string;
}

export interface EquippedTitleDto {
  key: string;
  name: string;
  displayPosition: 'Prefix' | 'Suffix';
  displayName: string;
}
