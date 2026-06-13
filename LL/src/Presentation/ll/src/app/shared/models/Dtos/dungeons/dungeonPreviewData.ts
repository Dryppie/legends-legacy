import { DungeonDifficulty } from '../../enums/dungeonDifficulty';
import { ItemInstance } from '../../item';
import { DungeonKeyItem } from './dungeonKeyItem';

export interface DungeonPreviewReward extends ItemInstance {
  source?: string;
  category?: string;
}

export interface DungeonRecord {
  hasCleared: boolean;
  firstClearedAt?: string | null;
  lastClearedAt?: string | null;
  totalClears: number;
}

export interface DungeonEntryRequirement {
  itemId: string;
  name: string;
  requiredAmount: number;
  ownedAmount: number;
  consumedOnEntry: boolean;
}

export interface DungeonPreviewData {
  id: string;
  familyId?: string;
  familyTitle?: string;
  number: number | string;
  title: string;
  difficulty?: DungeonDifficulty;
  grade?: string;
  recommendedCombatRating?: number;
  minimumCombatRating?: number;
  currentCombatRating?: number;
  canEnter?: boolean;
  readinessState?: string;
  missingRequirements?: string[];
  warnings?: string[];
  entryRequirements?: DungeonEntryRequirement[];
  requiredPreviousDungeonId?: string | null;
  heroImage: string;
  lore: string;
  requiredLevel: number;
  minRooms?: number;
  maxRooms?: number;
  dailyEntries?: number;
  keyItem?: DungeonKeyItem;
  roomsRange?: [number, number];
  record?: DungeonRecord;
  rewards: DungeonPreviewReward[];
  unlockedDifficulties: DungeonDifficulty[];
  difficultyVariants?: Partial<Record<DungeonDifficulty, DungeonPreviewData>>;
}
