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
}
