import { GatheringNode } from './Dtos/gatheringNode';
import { ItemType } from './enums/itemType';
import { ItemBase } from './item';

export interface Profession {
  name: string;
  iconPath: string;
}

export interface GatheringProfession extends Profession {
  gatheringNodes: GatheringNode[];
}

export interface CraftingProfession extends Profession {
  recipes: Recipe[];
}

export interface Recipe {
  id: string;
  name: string;
  item: ItemBase;
  quantity: number;
  levelRequirement: number;
  materials: Material[];
  itemType: ItemType;
}

export interface Material {
  quantity: number;
  item: ItemBase;
}
