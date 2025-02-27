import { CharacterActionType } from '../enums/characterActionType';
import { GatheringType } from '../enums/gatheringType';
import { CombatResultDto } from './combatResultDto';
import { Area } from './regionDto';

export interface CharacterActionDto {
  characterActionType: CharacterActionType;
  lootTableId: string;
  updatedAt: Date;
  isDeleted: boolean;
  combatResult?: CombatResultDto;
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

export interface CombatActionDetails {
  characterTeam: string[]; // or appropriate type
  area: Area;
}

export interface GatheringActionDetails {
  name: string;
  gatheringType: GatheringType;
}
