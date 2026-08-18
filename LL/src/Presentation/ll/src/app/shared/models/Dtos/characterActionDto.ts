import { CharacterActionType } from '../enums/characterActionType';
import { CraftingQueueItem } from '../profession';
import { CombatSessionDto } from './combatResultDto';
import { Area } from './regionDto';
import { TemperingSessionDto } from './temperingSessionDto';

export interface CharacterActionDto {
  characterActionType: CharacterActionType;
  lootTableId: string;
  updatedAt: Date;
  nextResolutionAtUtc?: Date | null;
  blockedUntilUtc?: Date | null;
  /** @deprecated compatibility alias; new clients use nextResolutionAtUtc. */
  nextResolutionAt?: Date | null;
  resolutionIntervalMs?: number | null;
  hasMoreDueWork?: boolean;
  processedCount?: number;
  scheduleGeneration?: number;
  revision: string;
  hasPendingCombatResolution?: boolean;
  isDeleted: boolean;
  temperingSession?: TemperingSessionDto;
  combatSession?: CombatSessionDto;
  craftingActionDetails?: CraftingActionDetails;
  temperingQueueItems?: CraftingQueueItem[];
  combatActionDetails?: CombatActionDetails;
}

export interface StartCombatActionRequest {
  areaId: string;
}

export interface StartCraftingActionRequest {
  queueId: string;
  itemInstanceId: string;
}

export interface CombatActionDetails {
  characterTeam: string[];
  area: Area;
}

export interface CraftingActionDetails {
  craftingQueueItems: CraftingQueueItem[];
}
