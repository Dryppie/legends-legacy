import { DungeonDifficulty } from '../../enums/dungeonDifficulty';
import { ItemInstance } from '../../item';
import { DungeonKeyItem } from './dungeonKeyItem';

export interface DungeonPreviewReward extends ItemInstance {
  source?: string;
  category?: string;
  minQuantity?: number;
  maxQuantity?: number;
  dropChancePercent?: number | null;
  canDropNothing?: boolean;
  noDropChancePercent?: number | null;
}

export interface DungeonGatheringLootPreview extends ItemInstance {
  itemId: string;
  minQuantity: number;
  maxQuantity: number;
  dropChancePercent: number;
  isRare: boolean;
}

export interface DungeonGatheringNodePreview {
  id: string;
  name: string;
  type: string;
  levelRequirement?: number | null;
  procChance: number;
  loot: DungeonGatheringLootPreview[];
}

export interface DungeonRecord {
  hasCleared: boolean;
  firstClearedAt?: string | null;
  lastClearedAt?: string | null;
  totalClears: number;
}

export interface DungeonMastery {
  experience: number;
  level: number;
  experienceRequiredForNextLevel?: number | null;
  completionCount: number;
  benefits?: DungeonMasteryBenefitSummary;
  benefitLevels?: DungeonMasteryBenefitLevel[];
}

export interface DungeonMasteryBenefitSummary {
  additionalVisibilityRows: number;
  restSiteVigorBonus: number;
  combatVigorCostReduction: number;
  completionCurrencyBonusPercent: number;
}

export interface DungeonMasteryBenefitLevel {
  level: number;
  id: string;
  name: string;
  description: string;
}

export interface DungeonEntryRequirement {
  itemId: string;
  name: string;
  requiredAmount: number;
  ownedAmount: number;
  description?: string | null;
}

export interface DungeonPreviewData {
  id: string;
  region: number;
  familyId?: string;
  familyTitle?: string;
  number: number | string;
  title: string;
  difficulty?: DungeonDifficulty;
  grade?: string;
  canEnter?: boolean;
  missingRequirements?: string[];
  entryRequirements?: DungeonEntryRequirement[];
  sigilItemId?: string | null;
  sigilName?: string | null;
  canAssembleSigil?: boolean;
  sigilAssemblyMissingRequirements?: string[];
  requiredTowerFloor?: number | null;
  requiredPreviousDungeonId?: string | null;
  lore: string;
  minRooms?: number;
  maxRooms?: number;
  dailyEntries?: number;
  keyItem?: DungeonKeyItem;
  roomsRange?: [number, number];
  record?: DungeonRecord;
  mastery?: DungeonMastery;
  rewards: DungeonPreviewReward[];
  unlockedDifficulties: DungeonDifficulty[];
  difficultyVariants?: Partial<Record<DungeonDifficulty, DungeonPreviewData>>;
}

export interface DungeonHubData {
  sigilFragments: number;
  sigilAssemblyEnabled: boolean;
  sigilAssemblyCost: number;
  dungeons: DungeonPreviewData[];
}
