export type GuildShopStockType = 'Common' | 'Weekly' | 'Prestige';

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
  guildHonorsCost: number;
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
  guildHonors: number;
  weeklyPeriodKey: string;
  nextWeeklyResetAt: string;
  items: GuildShopItem[];
}
