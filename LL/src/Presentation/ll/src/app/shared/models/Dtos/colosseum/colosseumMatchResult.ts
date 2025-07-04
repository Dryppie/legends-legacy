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
}
