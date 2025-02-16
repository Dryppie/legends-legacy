import { ItemType } from './enums/itemType';
import { Rarity } from './enums/rarity';
import { Essence } from './essence';

export interface Item {
  id: string;
  name: string;
  rarity: Rarity;
  itemType: ItemType;
  description: string;
  // ability: Ability;
  iconPath: string;
}

export interface EssenceItem extends Item {
  essence: Essence;
}
