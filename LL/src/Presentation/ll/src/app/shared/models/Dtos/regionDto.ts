import { GatheringType } from '../enums/gatheringType';

export interface Region {
  name: string;
  areas: Area[];
  dungeons: Dungeon[];
  raids: Raid[];
}

export interface Area {
  id: string;
  name: string;
  levelRequirement: number;
  creatures: string[];
  description: string;
  gatheringTypes?: GatheringType[];
  gatheringNodes?: AreaGatheringNode[];
  // creatures: Creature[];
}

export interface AreaGatheringNode {
  id: string;
  name: string;
  type: GatheringType;
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
  creatures: string[];
  // creatures: Creature[];
}
