import { CharacterActionType } from '../enums/characterActionType';
import { GatheringType } from '../enums/gatheringType';
import { CombatSessionDto } from './combatResultDto';
import { Area } from './regionDto';

export interface CharacterActionDto {
  characterActionType: CharacterActionType;
  lootTableId: string;
  updatedAt: Date;
  isDeleted: boolean;
  combatSession?: CombatSessionDto;
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
