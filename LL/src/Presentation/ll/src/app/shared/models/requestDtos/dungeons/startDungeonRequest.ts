import { DungeonDifficulty } from '../../enums/dungeonDifficulty';

export interface StartDungeonRequest {
  dungeonId: string;
  dungeonTier: DungeonDifficulty;
}
