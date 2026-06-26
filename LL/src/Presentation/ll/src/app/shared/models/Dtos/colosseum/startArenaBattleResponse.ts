import { CombatResultDto } from '../combatResultDto';
import { ArenaOpponentPreview } from './arenaOpponentPreview';
import { ArenaTicketStatus } from './arenaTicketStatus';
import { ArenaRankProgress } from './colosseumStatus';

export interface ArenaBattleOutcome {
  result: 'Victory' | 'Defeat' | 'Draw';
  attackerCharacterId: string;
  defenderCharacterId: string;
  winnerCharacterId?: string | null;
  completedAt: Date;
}

export interface ArenaRatingChange {
  ratingBefore: number;
  ratingAfter: number;
  delta: number;
}

export interface ArenaReward {
  gloryEarned: number;
  baseReward: number;
  dailyFirstWinBonus: number;
  streakBonus: number;
  defensiveBonus: number;
}

export interface ArenaRankChange {
  before: ArenaRankProgress;
  after: ArenaRankProgress;
  tierChanged: boolean;
}

export interface ArenaStreakChange {
  before: number;
  after: number;
  bonusGlory: number;
}

export interface StartArenaBattleResponse {
  battleId: string;
  battle: CombatResultDto;
  combat: CombatResultDto;
  outcome: ArenaBattleOutcome;
  arenaTicketStatus: ArenaTicketStatus;
  rewards: ArenaReward;
  attackerRating: ArenaRatingChange;
  defenderRating: ArenaRatingChange;
  attackerRank: ArenaRankChange;
  streak: ArenaStreakChange;
  opponent: ArenaOpponentPreview;
}
