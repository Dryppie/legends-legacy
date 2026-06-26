export interface ArenaRankProgress {
  currentTierId: string;
  currentTierName: string;
  rating: number;
  currentTierMinRating: number;
  nextTierMinRating?: number | null;
  nextTierName?: string | null;
  ratingUntilNextTier?: number | null;
  progressPercent: number;
}

export interface ArenaRecord {
  wins: number;
  draws: number;
  losses: number;
}

export interface ColosseumStatus {
  rating: number;
  lifetimeHighestRating: number;
  rankProgress: ArenaRankProgress;
  glory: number;
  tickets: number;
  maxTickets: number;
  nextTicketAt?: Date | null;
  currentAttackWinStreak: number;
  bestAttackWinStreak: number;
  dailyFirstWinAvailable: boolean;
  dailyFirstWinBonusGlory: number;
  attackRecord: ArenaRecord;
  defenseRecord: ArenaRecord;
  defenseStatus: ArenaDefenseStatus;
}

export interface ArenaDefenseStatus {
  hasSnapshot: boolean;
  isValid: boolean;
  isOutdated: boolean;
  updatedAt?: Date | null;
  loadoutHash?: string | null;
}
