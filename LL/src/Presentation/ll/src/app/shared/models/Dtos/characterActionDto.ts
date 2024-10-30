import { CombatResultDto } from './combatResultDto';

export interface CharacterActionDto {
  characterActionType: number;
  lootTableId: string;
  updatedAt: Date;
  combatResult?: CombatResultDto;
}
