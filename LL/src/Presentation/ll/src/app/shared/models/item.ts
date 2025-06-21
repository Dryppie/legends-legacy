import { AttributeModifier } from './Dtos/attributesDto';
import { AttributeType } from './enums/attributeType';
import { EquipmentType } from './enums/equipmentType';
import { ItemType } from './enums/itemType';
import { Rarity } from './enums/rarity';
import { Essence } from './essence';

export interface ItemInstance {
  id: string;
  itemBase: ItemBase;
}

export interface EquipmentInstance extends ItemInstance {
  rarity: Rarity;
  itemBase: Equipment;
  potential?: number;
  itemXp: number;
  attributeModifiers: AttributeModifier[];
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
  magnitude: number;
  scalingAttribute: AttributeType;
  scalingAmount: number;
}

export interface EssenceItem extends ItemBase {
  essence: Essence;
}
