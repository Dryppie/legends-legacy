export function canSpendEssenceDust(
  level: number,
  levelCap: number,
  dustHeld: number,
  spendingDust = false,
): boolean {
  return !spendingDust && level < levelCap && dustHeld > 0;
}

export function essenceDustLevelingDescription(
  level: number,
  levelCap: number,
  hasNextAscensionTier: boolean,
  dustHeld: number,
): string {
  if (level >= levelCap) {
    return hasNextAscensionTier
      ? `Level ${level} / ${levelCap}. Current level cap reached — Ascend to unlock more levels.`
      : `Level ${level} / ${levelCap}. Maximum Essence level reached.`;
  }

  return `1 Dust grants 1 level. Level ${level} / ${levelCap}. You have ${dustHeld} Dust.`;
}

export function essenceDustActionLabel(
  level: number,
  levelCap: number,
  dustHeld: number,
  spendingDust = false,
): string {
  if (spendingDust) return 'Leveling Up…';
  if (level >= levelCap) return 'Level Cap Reached';
  if (dustHeld <= 0) return 'No Dust Available';
  return 'Level Up · 1 Dust';
}
