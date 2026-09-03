import { CharacterActionType } from '../enums/characterActionType';
import { CombatSessionDto } from './combatResultDto';
import { Area } from './regionDto';

export interface CharacterActionDto {
  characterActionType: CharacterActionType;
  lootTableId: string;
  updatedAt: Date;
  nextResolutionAtUtc?: Date | null;
  blockedUntilUtc?: Date | null;
  resolutionIntervalMs?: number | null;
  hasMoreDueWork?: boolean;
  processedCount?: number;
  scheduleGeneration?: number;
  revision: string;
  isDeleted: boolean;
  combatSession?: CombatSessionDto;
  combatActionDetails?: CombatActionDetails;
}

export interface StartCombatActionRequest {
  areaId: string;
}

export interface CombatActionDetails {
  characterTeam: string[];
  area: Area;
}

