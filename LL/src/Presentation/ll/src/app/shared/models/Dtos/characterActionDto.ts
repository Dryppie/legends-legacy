import { CharacterActionType } from '../enums/characterActionType';
import { GatheringType } from '../enums/gatheringType';
import { CraftingQueueItem } from '../profession';
import { CombatSessionDto } from './combatResultDto';
import { Area } from './regionDto';
import { TemperingSessionDto } from './temperingSessionDto';

export interface CharacterActionDto {
  characterActionType: CharacterActionType;
  lootTableId: string;
  updatedAt: Date;
  isDeleted: boolean;
  temperingSession?: TemperingSessionDto;
  combatSession?: CombatSessionDto;
  craftingActionDetails?: CraftingActionDetails;
  combatActionDetails?: CombatActionDetails;
  gatheringActionDetails?: GatheringActionDetails;
}

export interface StartCombatActionRequest {
  areaId: string;
}

export interface StartGatheringActionRequest {
  gatheringNodeId: string;
  gatheringType: GatheringType;
}

export interface StartCraftingActionRequest {
  queueId: string;
  itemInstanceId: string;
}

export interface CombatActionDetails {
  characterTeam: string[]; // or appropriate type
  area: Area;
}

export interface GatheringActionDetails {
  name: string;
  gatheringType: GatheringType;
}

export interface CraftingActionDetails {
  craftingQueueItems: CraftingQueueItem[];
}
