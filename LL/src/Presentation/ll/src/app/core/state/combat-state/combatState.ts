import { CombatEvent } from '../../../shared/models/Dtos/combatEventDto';
import {
  BattleOutcome,
  CombatResultDto,
  EntityStats,
  SimpleCombatEntityDto,
} from '../../../shared/models/Dtos/combatResultDto';

export enum BattleType {
  IdleCombat = 'IdleCombat',
  Colosseum = 'Colosseum',
  Dungeon = 'Dungeon',
  Training = 'Training',
}

export interface CombatState {
  playerCharacters: SimpleCombatEntityDto[];
  enemyCharacters: SimpleCombatEntityDto[];
  combatEvents: CombatEvent[];
  combatResult: CombatResultDto | null;
  combatOutcome: BattleOutcome | null;
  nextCombat: Date | null;
  isCombatActive: boolean;
  entityStats: EntityStats[];
}
