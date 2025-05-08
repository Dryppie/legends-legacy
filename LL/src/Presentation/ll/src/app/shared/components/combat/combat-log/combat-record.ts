import { BattleOutcome } from '../../../models/Dtos/combatResultDto';

export interface CombatRecord {
  outcome: BattleOutcome;
  // gold: number;
  xp: number;
}
