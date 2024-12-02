import { ItemType } from './enums/itemType';
import { Rarity } from './enums/Rarity';
import { Essence } from './essence';

export interface Item {
  id: string;
  name: string;
  rarity: Rarity;
  itemType: ItemType;
  description: string;
  // ability: Ability;
  icon: string;
}

export interface EssenceItem extends Item {
  essence: Essence;
}
