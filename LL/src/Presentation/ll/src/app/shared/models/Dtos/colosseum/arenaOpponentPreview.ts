export interface ArenaOpponentPreview {
  opponentId: string;
  characterId: string; // Guid
  name: string;
  level: number;
  opponentRating: number;
  rankTier: string;
  rankTierId: string;
  challengeAvailableAt?: Date | string | null;

  deltaIfVictory: number;
  deltaIfDefeat: number;
  deltaIfDraw: number;
  gloryIfVictory: number;
  gloryIfDraw: number;
  gloryIfDefeat: number;
}
