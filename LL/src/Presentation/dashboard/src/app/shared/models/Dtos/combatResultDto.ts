import { CombatEvent } from './combatEventDto';

export interface CombatResultDto {
  playerTeam: SimpleCombatEntityDto[];
  enemyTeam: SimpleCombatEntityDto[];
  duration: number;
  eventLog: CombatEvent[];
  startedAt: Date;
  outcome: BattleOutcome;
  experienceGained: number;
}

export interface SimpleCombatEntityDto {
  name: string;
  id: string;
  imagePath: string;
  health: number;
  maxHealth: number;
  barrier: number;
}

export enum BattleOutcome {
  Victory = 'Victory',
  Defeat = 'Defeat',
  Draw = 'Draw',
}
