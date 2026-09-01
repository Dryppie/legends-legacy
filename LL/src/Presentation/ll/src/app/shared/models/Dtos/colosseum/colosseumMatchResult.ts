import { CombatResultDto } from '../combatResultDto';

export interface ColosseumMatchResult {
  characterAId: string;
  characterAName: string;
  characterARatingBefore: number;
  characterARatingAfter: number;
  characterBId: string;
  characterBName: string;
  characterBRatingBefore: number;
  characterBRatingAfter: number;
  winnerId: string;
  winnerName: string;
  playedAt: Date;
  outcome: string;
  characterARatingDelta: number;
  characterBRatingDelta: number;
  characterAGloryEarned: number;
  characterBGloryEarned: number;
  characterAStreakBefore: number;
  characterAStreakAfter: number;
  combatSummary?: CombatResultDto | null;
}
