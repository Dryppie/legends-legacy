import { BattleType } from '../../../core/state/combat-state/combatState';
import { GatheringType } from '../enums/gatheringType';
import { Rarity } from '../enums/rarity';
import { InventoryItem } from '../inventoryItem';

export interface CombatResultDto {
  playerTeam: SimpleCombatEntityDto[];
  enemyTeam: SimpleCombatEntityDto[];
  duration: number;
  startedAt: Date;
  outcome: BattleOutcome;
  loot: InventoryItem[];
  gatheringRewards: GatheringRewardResult[];
  experienceGained: number;
  battleType: BattleType;
  entityStats: EntityStats[];
}

export interface GatheringRewardResult {
  toolType: GatheringType;
  nodeId: string;
  nodeName: string;
  toolName: string;
  toolRarity: Rarity;
  success: boolean;
  itemsGained: InventoryItem[];
  appliedBonusEffects: string[];
  message?: string;
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
  selfDamageDone: number;
  selfDamageTaken: number;
  alliedDamageDone: number;
  alliedDamageTaken: number;
  team: string;
}

export interface AbilityStats {
  name: string;
  totalDamage: number;
  totalHealing: number;
  uses: number;
  hits: number;
  crits: number;
  summons: number;
  stuns: number;
  selfDamage: number;
  alliedDamage: number;
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
