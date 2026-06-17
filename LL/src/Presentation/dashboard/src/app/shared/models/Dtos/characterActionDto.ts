import { CharacterActionType } from '../enums/characterActionType';
import { CombatResultDto } from './combatResultDto';
import { Area } from './regionDto';

export interface CharacterActionDto {
  characterActionType: CharacterActionType;
  lootTableId: string;
  updatedAt: Date;
  isDeleted: boolean;
  combatResult?: CombatResultDto;
  combatActionDetails?: CombatActionDetails;
}

export interface StartCombatActionRequest {
  areaId: string;
}

export interface CombatActionDetails {
  characterTeam: string[];
  area: Area;
}
