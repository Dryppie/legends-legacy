import { ProfessionType } from './Dtos/characterProfession';
import { GatheringNode } from './Dtos/gatheringNode';
import { ItemType } from './enums/itemType';
import { Equipment, EquipmentInstance, ItemBase } from './item';

export interface Profession {
  name: string;
  iconPath: string;
  professionType: ProfessionType;
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
  item: Equipment;
  quantity: number;
  craftType: CraftType;
  levelRequirement: number;
  materials: Material[];
  itemType: ItemType;
}

export interface CraftingQueueItem {
  id: string;
  equipment: EquipmentInstance;
  startedAt: Date;
  status: CraftingQueueStatus;
}

export enum CraftingQueueStatus {
  Queued = 'Queued',
  InProgress = 'InProgress',
  Completed = 'Completed',
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
