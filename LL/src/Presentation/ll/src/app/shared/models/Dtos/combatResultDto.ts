import { BattleType } from '../../../core/state/combat-state/combatState';
import { CombatEvent } from './combatEventDto';

export interface CombatResultDto {
  playerTeam: SimpleCombatEntityDto[];
  enemyTeam: SimpleCombatEntityDto[];
  duration: number;
  eventLog: CombatEvent[];
  startedAt: Date;
  outcome: BattleOutcome;
  experienceGained: number;
  battleType: BattleType;
  entityStats: EntityStats[];
}

export interface EntityStats {
  entityId: string;
  entityName: string;
  abilities: AbilityStats[];
  damageDone: number;
  damageTaken: number;
  healingDone: number;
  healingReceived: number;
  healthRegenerated: number;
}

export interface AbilityStats {
  name: string;
  totalDamage: number;
  totalHealing: number;
  hits: number;
  crits: number;
  summons: number;
  stuns: number;
}

export interface SimpleCombatEntityDto {
  name: string;
  id: string;
  imagePath: string;
  health: number;
  maxHealth: number;
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
  totalCinders: number;
  totalSoulstones: number;
}

export enum BattleOutcome {
  Victory = 'Victory',
  Defeat = 'Defeat',
  Draw = 'Draw',
}
