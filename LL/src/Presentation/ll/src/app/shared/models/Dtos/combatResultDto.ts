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
  mana: number;
  maxMana: number;
  barrier: number;
}

export interface CombatSessionDto {
  from: Date;
  to: Date;

  combatResult: CombatResultDto;
  combatSummary: SessionSummary;
}

export interface SessionSummary {
  totalBattles: number;
  wins: number;
  losses: number;
  draws: number;
  totalExperience: number;
  totalGold: number;
}

export enum BattleOutcome {
  Victory = 'Victory',
  Defeat = 'Defeat',
  Draw = 'Draw',
}
