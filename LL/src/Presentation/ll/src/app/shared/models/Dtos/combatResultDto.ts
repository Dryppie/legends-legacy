import { CombatEvent } from './combatEventDto';

export interface CombatResultDto {
  playerTeam: CombatEntityDto[];
  enemyTeam: CombatEntityDto[];
  duration: number;
  eventLog: CombatEvent[];
  startedAt: Date;
  outcome: BattleOutcome;
}

export interface CombatEntityDto {
  name: string;
  id: string;
  health: number;
  maxHealth: number;
  mana: number;
  maxMana: number;
}

export enum BattleOutcome {
  Victory,
  Defeat,
  Draw,
}
