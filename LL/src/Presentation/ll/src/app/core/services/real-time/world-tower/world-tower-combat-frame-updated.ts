import { TowerCombatFrame } from '../../api/world-tower/world-tower.service';

export interface WorldTowerCombatFrameUpdated {
  attemptId: string;
  rallyId: string;
  playbackStartedAt: string;
  ticksPerSecond: number;
  ticksPerFrame: number;
  frame: TowerCombatFrame;
}
