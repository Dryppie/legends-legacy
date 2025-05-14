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
  readonly recipes: Recipe[];
}

export interface Recipe {
  id: string;
  name: string;
  itemId: string;
  item: ItemBase;
  quantity: number;
  craftType: CraftType;
  levelRequirement: number;
  materials: Material[];
  itemType: ItemType;
}

export interface Material {
  recipeId: string;
  quantity: number;
  itemId: string;
  item: ItemBase;
}

export enum CraftType {
  ArmorForging = 'ArmorForging',
  JewelryCrafting = 'JewelryCrafting',
  WeaponSmithing = 'WeaponSmithing',
}
