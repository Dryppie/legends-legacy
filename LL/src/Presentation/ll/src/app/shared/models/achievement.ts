export type AchievementCategory =
  | 'General'
  | 'Combat'
  | 'Essences'
  | 'Dungeons'
  | 'Colosseum'
  | 'WorldTower'
  | 'Guild'
  | 'Raids'
  | 'Prophecies'
  | 'Economy'
  | 'Seasonal'
  | 'Hidden'
  | 'Legacy'
  | 'ServerFirst';

export type AchievementVisibility = 'Visible' | 'Hidden' | 'Obscured' | 'Legacy';
export type TitleRarity = 'Common' | 'Renowned' | 'Exalted' | 'Legendary' | 'Mythic';
export type TitleDisplayPosition = 'Prefix' | 'Suffix';
export type TitleScope = 'Account' | 'Character' | 'Seasonal' | 'Server';

export interface AchievementDto {
  key: string;
  name: string;
  description: string;
  hint?: string | null;
  category: AchievementCategory;
  type: string;
  scope: string;
  visibility: AchievementVisibility;
  rarity: TitleRarity;
  requirementType: string;
  requirementTarget?: string | null;
  requiredAmount: number;
  currentAmount: number;
  points: number;
  isCompleted: boolean;
  completedAt?: string | null;
  completedByCharacterId?: string | null;
  rewardTitleKey?: string | null;
  rewardTitleName?: string | null;
}

export interface AchievementOverviewDto {
  totalAchievementPoints: number;
  legacyRenownRank: number;
  legacyRenownName: string;
  totalAchievementsUnlocked: number;
  totalAchievementsAvailable: number;
  totalTitlesUnlocked: number;
  recentlyUnlockedAchievements: AchievementDto[];
  nearlyCompletedAchievements: AchievementDto[];
  categorySummaries: AchievementCategorySummaryDto[];
}

export interface AchievementCategorySummaryDto {
  category: AchievementCategory;
  unlocked: number;
  available: number;
  currentProgress: number;
  requiredProgress: number;
}

export interface TitleDto {
  key: string;
  name: string;
  description: string;
  category: AchievementCategory;
  rarity: TitleRarity;
  displayPosition: TitleDisplayPosition;
  scope: TitleScope;
  isUnlocked: boolean;
  isEquipped: boolean;
  sourceAchievementKey?: string | null;
  unlockedByCharacterId?: string | null;
  unlockedAt?: string | null;
  preview: string;
  prefixPreview: string;
  suffixPreview: string;
}

export interface EquippedTitleDto {
  key: string;
  name: string;
  displayPosition: TitleDisplayPosition;
  displayName: string;
}
