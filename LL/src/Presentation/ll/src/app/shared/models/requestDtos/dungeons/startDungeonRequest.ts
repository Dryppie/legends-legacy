import { DungeonDifficulty } from '../../enums/dungeonDifficulty';

export interface StartDungeonRequest {
  dungeonId: string;
  difficulty: DungeonDifficulty;
}
