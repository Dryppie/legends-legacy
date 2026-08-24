import { AttributeModifier } from './Dtos/attributesDto';
import { EquipmentType } from './Dtos/equipmentSlot';
import { ItemType } from './enums/itemType';
import { ItemQuality } from './enums/itemQuality';
import { Rarity } from './enums/rarity';
import { Essence } from './essence';

export interface ItemInstance {
  id: string;
  itemBase: ItemBase;
}

export interface EquipmentInstance extends ItemInstance {
  rarity: Rarity;
  quality: ItemQuality;
  baseRecipeId?: string | null;
  blueprintId?: string | null;
  tier: number;
  potential?: number;
  maxPotential?: number | null;
  temperingProgress: number;
  affinityTags: string[];
}

export interface ItemBase {
  id: string;
  name: string;
  iconPath: string;
  rarity: Rarity;
  itemType: ItemType;
  description: string;
  stackable: boolean;
  isBound?: boolean;
  equipmentType: EquipmentType;
  attributeModifiers: AttributeModifier[];
}

export interface Equipment extends ItemBase {
  equipmentType: EquipmentType;
  attributeModifiers: AttributeModifier[];
}

export interface EssenceItem extends ItemBase {
  essence?: Essence;
  essenceDefinitionId: string;
  dismantleDustAmount: number;
}

export function essenceItemToEssence(item: EssenceItem): Essence {
  return (
    item.essence ?? {
      name: item.name,
      active: {
        name: 'Unbound Essence',
        description: item.description,
        effectTypes: [],
        targeting: [],
        cooldown: 0,
      },
      passive: {
        name: 'Soul Archive',
        description: 'Absorb this item to add it to the Soul Archive.',
        effectTypes: [],
        targeting: [],
        cooldown: 0,
      },
      attributeModifiers: [],
    }
  );
}
