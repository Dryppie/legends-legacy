import { InventoryItem } from '../../inventoryItem';

export type GuildShopStockType = 'Common' | 'Rare';

export interface GuildShopReward {
  type: string;
  amount: number;
  key?: string | null;
  name?: string | null;
  description?: string | null;
}

export interface GuildShopItem {
  key: string;
  name: string;
  description: string;
  stockType: GuildShopStockType;
  guildFavorCost: number;
  weeklyLimit: number;
  purchasedThisPeriod: number;
  requiredWeeklyContribution: number;
  requiredMarketOfficeLevel: number;
  isInWeeklyRotation: boolean;
  rewards: GuildShopReward[];
  canPurchase: boolean;
  lockedReason?: string | null;
}

export interface GuildShopOverview {
  guildId: string;
  guildFavor: number;
  weeklyPeriodKey: string;
  nextWeeklyResetAt: string;
  items: GuildShopItem[];
}

export interface GuildShopPurchaseResponse extends GuildShopOverview {
  inventoryGrantId?: string | null;
  inventoryItemsGranted?: InventoryItem[];
}
