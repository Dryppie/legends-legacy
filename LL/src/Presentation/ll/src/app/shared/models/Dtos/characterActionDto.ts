import { CharacterActionType } from '../enums/characterActionType';
import { GatheringType } from '../enums/gatheringType';
import { CombatResultDto } from './combatResultDto';

export interface CharacterActionDto {
  characterActionType: CharacterActionType;
  lootTableId: string;
  updatedAt: Date;
  combatResult?: CombatResultDto;
  combatActionDetails: CombatActionDetails;
  gatheringActionDetails: GatheringActionDetails;
}

export interface StartCombatActionRequest {
  areaName: string;
}

export interface StartGatheringActionRequest {
  gatheringActionDetails: GatheringActionDetails;
}

export interface CombatActionDetails {
  characterTeam: string[]; // or appropriate type
  enemyTeam: string[]; // or appropriate type
}

export interface GatheringActionDetails {
  name: string;
  gatheringType: GatheringType;
  lootTableId: string;
}
