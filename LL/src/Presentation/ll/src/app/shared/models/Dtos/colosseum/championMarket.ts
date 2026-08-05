export interface ChampionMarketItem {
  id: string;
  name: string;
  description: string;
  category: string;
  gloryCost: number;
  weeklyPurchaseLimit?: number | null;
  lifetimePurchaseLimit?: number | null;
  remainingWeeklyPurchases: number;
  remainingLifetimePurchases: number;
  requiredRating?: number | null;
  requiredRankTier?: string | null;
  canPurchase: boolean;
  cannotPurchaseReason?: string | null;
  sortOrder: number;
  cindersGranted: number;
  soulstonesGranted: number;
  sigilFragmentsGranted: number;
  rewardItemId?: string | null;
  rewardItemName?: string | null;
  rewardItemQuantity: number;
}

export interface ChampionMarket {
  items: ChampionMarketItem[];
  glory: number;
  weeklyResetAt: Date;
}

export interface ChampionMarketPurchaseResponse {
  itemId: string;
  quantity: number;
  glorySpent: number;
  gloryRemaining: number;
  cindersGranted: number;
  soulstonesGranted: number;
  sigilFragmentsGranted: number;
  rewardItemId?: string | null;
  rewardItemName?: string | null;
  rewardItemQuantity: number;
}
