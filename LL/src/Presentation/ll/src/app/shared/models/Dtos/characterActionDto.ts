import { CharacterActionType } from '../enums/characterActionType';
import { GatheringType } from '../enums/gatheringType';
import { CraftingQueueItem } from '../profession';
import { ProfessionType } from './characterProfession';
import { CombatSessionDto } from './combatResultDto';
import { GatheringSessionDto } from './gatheringSessionDto';
import { Area } from './regionDto';
import { TemperingSessionDto } from './temperingSessionDto';

export interface CharacterActionDto {
  characterActionType: CharacterActionType;
  lootTableId: string;
  updatedAt: Date;
  isDeleted: boolean;
  temperingSession?: TemperingSessionDto;
  combatSession?: CombatSessionDto;
  gatheringSession?: GatheringSessionDto;
  craftingActionDetails?: CraftingActionDetails;
  combatActionDetails?: CombatActionDetails;
  gatheringActionDetails?: GatheringActionDetails;
}

export interface StartCombatActionRequest {
  areaId: string;
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
  professionType: GatheringType;
}

export interface CraftingActionDetails {
  craftingQueueItems: CraftingQueueItem[];
}
