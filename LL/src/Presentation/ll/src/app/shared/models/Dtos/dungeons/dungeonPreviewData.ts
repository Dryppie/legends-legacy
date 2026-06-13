import { DungeonDifficulty } from '../../enums/dungeonDifficulty';
import { ItemInstance } from '../../item';
import { DungeonKeyItem } from './dungeonKeyItem';

export interface DungeonPreviewReward extends ItemInstance {
  source?: string;
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
  missingRequirements?: string[];
  requiredPreviousDungeonId?: string | null;
  heroImage: string;
  lore: string;
  requiredLevel: number;
  minRooms?: number;
  maxRooms?: number;
  dailyEntries?: number;
  keyItem?: DungeonKeyItem;
  roomsRange?: [number, number];
  rewards: DungeonPreviewReward[];
  unlockedDifficulties: DungeonDifficulty[];
  difficultyVariants?: Partial<Record<DungeonDifficulty, DungeonPreviewData>>;
}
