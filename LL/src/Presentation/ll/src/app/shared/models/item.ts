import { AttributeModifier } from './Dtos/attributesDto';
import { EquipmentType } from './Dtos/equipmentSlot';
import { ItemType } from './enums/itemType';
import { Rarity } from './enums/rarity';
import { Essence } from './essence';

export interface ItemInstance {
  id: string;
  itemBase: ItemBase;
}

export interface EquipmentInstance extends ItemInstance {
  itemBase: Equipment;
  potential?: number;
}

export interface ItemBase {
  id: string;
  name: string;
  rarity: Rarity;
  itemType: ItemType;
  description: string;
  stackable: boolean;
}

export interface Equipment extends ItemBase {
  equipmentType: EquipmentType;
  attributeModifiers: AttributeModifier[];
}

export interface EssenceItem extends ItemBase {
  essence: Essence;
}
