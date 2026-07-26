export const COMBAT_RATING_DISPLAY_DIVISOR = 10;

export function toDisplayedCombatRating(rating: number): number {
  return Math.floor(rating / COMBAT_RATING_DISPLAY_DIVISOR);
}
