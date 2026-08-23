import { GatheringType } from '../enums/gatheringType';

export interface Region {
  name: string;
  requiredTowerFloor?: number;
  areas: Area[];
  dungeons: Dungeon[];
  raids: Raid[];
}

export interface Area {
  id: string;
  name: string;
  levelRequirement: number;
  creatures: string[];
  essenceProgress?: AreaEssenceProgress;
  possibleDrops?: AreaDrop[];
  gatheringTypes?: GatheringType[];
  gatheringNodes?: AreaGatheringNode[];
  // creatures: Creature[];
}

export interface AreaEssenceProgress {
  collected: number;
  total: number;
}

export interface AreaDrop {
  itemId: string;
  name: string;
}

export interface AreaGatheringNode {
  id: string;
  name: string;
  type: GatheringType;
  yieldBonusPercent?: number;
}

export interface Dungeon {
  id: string;
  name: string;
  creatures: string[];
  // creatures: Creature[];
}

export interface Raid {
  id: string;
  name: string;
  levelRequirement: number;
}
