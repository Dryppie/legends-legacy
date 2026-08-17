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
  requiredRankMinRating?: number | null;
  sortOrder: number;
  cindersGranted: number;
  soulstonesGranted: number;
  sigilFragmentsGranted: number;
  rewardItemId?: string | null;
  rewardItemName?: string | null;
  rewardItemQuantity: number;
}

export interface ChampionMarketItemView extends ChampionMarketItem {
  canPurchase: boolean;
  cannotPurchaseReason: string | null;
}

export interface ChampionMarket {
  items: ChampionMarketItem[];
  glory: number;
  weeklyResetAt: Date;
}

export interface ChampionMarketView extends Omit<ChampionMarket, 'items'> {
  items: ChampionMarketItemView[];
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
  inventoryGrantId?: string | null;
  inventoryItemsGranted?: InventoryItem[];
}
import { InventoryItem } from '../../inventoryItem';
