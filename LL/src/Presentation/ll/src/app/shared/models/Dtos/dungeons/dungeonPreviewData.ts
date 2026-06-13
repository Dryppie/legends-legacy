import { DungeonDifficulty } from '../../enums/dungeonDifficulty';
import { ItemInstance } from '../../item';
import { DungeonKeyItem } from './dungeonKeyItem';

export interface DungeonPreviewReward extends ItemInstance {
  source?: string;
}

export interface DungeonPreviewData {
  id: string;
  number: number | string;
  title: string;
  grade?: string;
  recommendedPowerScore?: number;
  minimumPowerScore?: number;
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
