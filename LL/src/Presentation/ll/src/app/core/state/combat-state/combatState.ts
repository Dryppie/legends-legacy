import { CombatEvent } from '../../../shared/models/Dtos/combatEventDto';
import {
  BattleOutcome,
  CombatResultDto,
  SimpleCombatEntityDto,
} from '../../../shared/models/Dtos/combatResultDto';

export enum BattleType {
  Idle = 'Idle',
  Colosseum = 'Colosseum',
}

export interface CombatState {
  playerCharacters: SimpleCombatEntityDto[];
  enemyCharacters: SimpleCombatEntityDto[];
  combatEvents: CombatEvent[];
  combatResult: CombatResultDto | null;
  combatOutcome: BattleOutcome | null;
  nextCombat: Date | null;
  isCombatActive: boolean;
}
