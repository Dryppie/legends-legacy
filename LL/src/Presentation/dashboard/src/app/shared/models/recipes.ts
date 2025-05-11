import { ItemType } from './enums/itemType';
import { ItemBase } from './item';

export interface Recipe {
  id: string;
  name: string;
  item: ItemBase;
  quantity: number;
  craftType: CraftType;
  levelRequirement: number;
  materials: Material[];
  itemType: ItemType;
}

export interface Material {
  quantity: number;
  item: ItemBase;
}

export enum CraftType {
  ArmorForging = 'ArmorForging',
  JewelryCrafting = 'JewelryCrafting',
  WeaponSmithing = 'WeaponSmithing',
}
