import { ItemBase } from './item';

export enum QuestStatus {
  Active = 'Active',
  Completed = 'Completed',
}

export interface QuestJournal {
  quests: QuestState[];
  pinnedQuestId?: string | null;
}

export interface QuestState {
  questId: string;
  version: number;
  title: string;
  summary: string;
  category: string;
  objectiveMode: 'Sequential' | 'All';
  chain?: QuestChain | null;
  choice?: QuestChoice | null;
  sortOrder: number;
  status: QuestStatus;
  isPinned: boolean;
  requiresWelcome: boolean;
  acceptedAt?: string | null;
  completedAt?: string | null;
  objectives: QuestObjectiveState[];
  rewards: QuestRewardState[];
}

export interface QuestChoice {
  selectionTitle: string;
  selectionSummary: string;
  confirmationText: string;
  selectedOptionKey?: string | null;
  options: QuestChoiceOption[];
}

export interface QuestChoiceOption {
  key: string;
  title: string;
  summary: string;
  creatureId: string;
  creatureName: string;
  essenceDefinitionId: string;
  rewardItemBaseId: string;
  encounterKey: string;
  rewardItemBase?: ItemBase | null;
}

export interface QuestChain {
  id: string;
  title: string;
  description: string;
  goal: string;
  promisedReward: string;
  step: number;
  totalSteps: number;
}

export interface QuestObjectiveState {
  key: string;
  description: string;
  type: string;
  currentAmount: number;
  requiredAmount: number;
  isCompleted: boolean;
  presentation: QuestPresentation;
}

export interface QuestPresentation {
  actionLabel: string;
  destinationRoute: string;
  guidePageId?: string | null;
  tourPageId?: string | null;
}

export interface QuestRewardState {
  key: string;
  type: string;
  itemBaseId?: string | null;
  quantity: number;
  itemBase?: ItemBase | null;
}

export interface CombatAreaAccess {
  areaId: string;
  canAccess: boolean;
  isVisible: boolean;
  requiredLevel: number;
  characterLevel?: number | null;
  requiredQuestIds: string[];
  unmetQuestIds: string[];
  requiredTowerFloor?: number | null;
  isRequiredTowerFloorCleared: boolean;
  reasonCode?: string | null;
  playerMessage?: string | null;
}

/** Category used by the new-player tutorial quest line. */
export const ONBOARDING_QUEST_CATEGORY = 'Tutorial';
export const TRAINING_DAY_QUEST_ID = 'quest.onboarding.training_day';
export const SOUL_ARCHIVE_QUEST_ID = 'quest.onboarding.soul_archive';
export const FIRST_WEAPON_QUEST_ID = 'quest.onboarding.first_weapon';
export const TOOLS_OF_THE_TRADE_QUEST_ID = 'quest.onboarding.tools_of_trade';
export const INTO_LUMO_RUINS_QUEST_ID = 'quest.region01.into_lumo_ruins';
export const HEART_OF_THE_HOLLOW_QUEST_ID =
  'quest.shenic.heart_of_the_hollow';
export const TRAINING_GROUNDS_AREA_ID = 'tutorial_area_training_grounds';
export const LUMO_RUINS_AREA_ID = 'region_01_area_01';
export const ONBOARDING_ONE_HANDED_WEAPON_ITEM_BASE_IDS: ReadonlySet<string> =
  new Set(['shortsword', 'dagger', 'hatchet', 'mace', 'wand']);
export const ONBOARDING_GATHERING_TOOL_ITEM_BASE_IDS: ReadonlySet<string> =
  new Set(['basic_pickaxe', 'basic_hatchet', 'basic_skinning_knife']);
export const ONBOARDING_GOBLIN_ESSENCE_DEFINITION_ID = 'essence.goblin';
