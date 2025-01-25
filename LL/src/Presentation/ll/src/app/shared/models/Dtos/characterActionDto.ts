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
  areaId: string;
}

export interface StartGatheringActionRequest {
  gatheringNodeId: string;
  gatheringType: GatheringType;
}

export interface CombatActionDetails {
  characterTeam: string[]; // or appropriate type
  enemyTeam: string[]; // or appropriate type
}

export interface GatheringActionDetails {
  name: string;
  gatheringType: GatheringType;
}
