import { ItemBase } from './item';

export enum EventQuestStatus {
  Upcoming = 'Upcoming',
  Active = 'Active',
  Completed = 'Completed',
  Ended = 'Ended',
  Expired = 'Expired',
}

export interface EventQuestJournal {
  events: EventQuestState[];
}

export interface EventQuestState {
  eventQuestId: string;
  version: number;
  title: string;
  summary: string;
  status: EventQuestStatus;
  startsAtUtc: string;
  endsAtUtc: string;
  claimEndsAtUtc: string;
  completedAt?: string | null;
  minimumContribution: number;
  myContribution: number;
  isEligible: boolean;
  hasClaimed: boolean;
  myContributionRank?: number | null;
  contributorCount: number;
  contributionToNextRank?: number | null;
  sortOrder: number;
  objectives: EventQuestObjectiveState[];
  rewards: EventQuestRewardState[];
  personalMilestones: EventQuestPersonalMilestoneState[];
  topContributors: EventQuestContributorState[];
}

export interface EventQuestObjectiveState {
  key: string;
  description: string;
  type: string;
  currentAmount: number;
  requiredAmount: number;
  isCompleted: boolean;
}

export interface EventQuestRewardState {
  key: string;
  type: string;
  itemBaseId?: string | null;
  quantity: number;
  itemBase?: ItemBase | null;
}

export interface EventQuestPersonalMilestoneState {
  key: string;
  requiredContribution: number;
  isUnlocked: boolean;
  isClaimed: boolean;
  rewards: EventQuestRewardState[];
}

export interface EventQuestContributorState {
  rank: number;
  characterId: string;
  characterName: string;
  contribution: number;
}
