export interface ArenaOpponentPreview {
  characterId: string; // Guid
  name: string;
  level: number;
  opponentRating: number;

  deltaIfVictory: number;
  deltaIfDefeat: number;
  deltaIfDraw: number;
}
