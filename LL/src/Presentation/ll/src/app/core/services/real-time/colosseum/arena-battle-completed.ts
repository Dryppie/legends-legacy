export interface ArenaBattleCompletedMsg {
  characterId: string;
  enemyId: string;
  outcome: string;
  characterRatingBefore: number;
  characterRatingAfter: number;
  enemyRatingBefore: number;
  enemyRatingAfter: number;
}
