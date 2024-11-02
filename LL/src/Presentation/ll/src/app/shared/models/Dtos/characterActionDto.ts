import { CombatResultDto } from './combatResultDto';

export interface CharacterActionDto {
  characterActionType: number;
  lootTableId: string;
  updatedAt: Date;
  combatResult?: CombatResultDto;
}

export interface StartCombatActionRequest {
  combatActionDetails: CombatActionDetails;
}

export interface StartGatheringActionRequest {
  gatheringActionDetails: GatheringActionDetails
}

interface ActionDetails {
  // Common properties if any
}

export interface CombatActionDetails extends ActionDetails {
  characterTeam: string[]; // or appropriate type
  enemyTeam: string[]; // or appropriate type
}

export interface GatheringActionDetails extends ActionDetails {
  lootTableId: string;
}
