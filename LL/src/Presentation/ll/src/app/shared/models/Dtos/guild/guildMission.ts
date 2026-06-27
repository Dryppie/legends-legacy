export type GuildContributionTier =
  | 'None'
  | 'Bronze'
  | 'Silver'
  | 'Gold'
  | 'Platinum';

export type GuildMissionStatus =
  | 'PendingSelection'
  | 'Active'
  | 'Completed'
  | 'Expired'
  | 'Finalized';

export type PersonalGuildOrderStatus =
  | 'Active'
  | 'Completed'
  | 'Expired'
  | 'RewardClaimed';

export interface GuildMissionDefinition {
  id: string;
  key: string;
  name: string;
  description: string;
  category: string;
  metric: string;
  baseTarget: number;
}

export interface GuildMissionOption {
  id: string;
  definition: GuildMissionDefinition;
  weekKey: string;
  expiresAt: string;
  isSelected: boolean;
}

export interface GuildMissionInstance {
  id: string;
  definition: GuildMissionDefinition;
  weekKey: string;
  targetAmount: number;
  currentAmount: number;
  status: GuildMissionStatus;
  startedAt: string;
  endsAt: string;
  rewardClaimDeadline: string;
}

export interface GuildMissionContribution {
  amount: number;
  tier: GuildContributionTier;
  lastContributedAt?: string | null;
  rewardClaimed: boolean;
  canClaimReward: boolean;
}

export interface PersonalGuildOrder {
  id: string;
  definition: GuildMissionDefinition;
  periodKey: string;
  targetAmount: number;
  currentAmount: number;
  status: PersonalGuildOrderStatus;
  canClaimReward: boolean;
  generatedAt: string;
  completedAt?: string | null;
}

export interface GuildContributionSummary {
  dailyPeriodKey: string;
  weeklyPeriodKey: string;
  dailyContributionScore: number;
  weeklyContributionScore: number;
  guildFavorEarned: number;
  guildXpGenerated: number;
  guildSuppliesGenerated: number;
  ordersCompleted: number;
}

export interface GuildContributionLeaderboardEntry {
  characterId: string;
  characterName: string;
  weeklyContributionScore: number;
  weeklyMissionContribution: number;
  guildFavorEarned: number;
  guildXpGenerated: number;
  guildSuppliesGenerated: number;
  ordersCompleted: number;
  lastContributedAt?: string | null;
}

export interface GuildMissionOverview {
  guildId: string;
  guildXp: number;
  guildLevel: number;
  nextDailyResetAt: string;
  nextWeeklyResetAt: string;
  canSelectMission: boolean;
  weeklyOptions: GuildMissionOption[];
  activeMission?: GuildMissionInstance | null;
  myWeeklyContribution?: GuildMissionContribution | null;
  personalOrders: PersonalGuildOrder[];
  contributionSummary: GuildContributionSummary;
  contributionLeaderboard: GuildContributionLeaderboardEntry[];
}
