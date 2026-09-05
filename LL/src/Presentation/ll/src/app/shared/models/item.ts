import { AttributeModifier } from './Dtos/attributesDto';
import { EquipmentType } from './enums/equipmentType';
import { ItemType } from './enums/itemType';
import { ItemQuality } from './enums/itemQuality';
import { Rarity } from './enums/rarity';
import { Essence } from './essence';
import { EssenceDefinitionDto } from './essence-system';
import { EquipmentProgression } from './equipment-progression';

export interface ItemInstance {
  id: string;
  itemBase: ItemBase;
  displayName?: string;
  source?: string;
  category?: string;
  isBound?: boolean;
}

export interface EquipmentInstance extends ItemInstance {
  progression?: EquipmentProgression | null;
  displayName: string;
  isFavorite?: boolean;
  rarity: Rarity;
  quality: ItemQuality;
  equipmentSet?: EquipmentSetMetadata | null;
  tier: number;
  requiredLevel?: number;
  equipmentBase: Equipment;
  baseModifiers: AttributeModifier[];
  instanceModifiers: AttributeModifier[];
  attributeModifiers: AttributeModifier[];
  effectiveAttributeModifiers?: AttributeModifier[];
  affinityTags: string[];
  itemBudget: number;
  itemBudgetTier: number;
  isGuildBorrowed: boolean;
  guildVaultItemId?: string | null;
  borrowedFromGuildName?: string | null;
}

export interface ItemBase {
  id: string;
  name: string;
  rarity: Rarity;
  itemType: ItemType;
  description: string;
  stackable: boolean;
  isBound?: boolean;
  selectionCrate?: SelectionCrateMetadata | null;
}

export interface SelectionCrateMetadata {
  selectionLabel: string;
  options: SelectionCrateOption[];
}

export interface SelectionCrateOption {
  id: string;
  name: string;
  quantity: number;
}

export interface EquipmentSetMetadata {
  id: string;
  name: string;
  description: string;
  bonuses: EquipmentSetBonusMetadata[];
}

export interface EquipmentSetBonusMetadata {
  id: string;
  requiredEquippedItems: number;
  description: string;
}

export interface Equipment extends ItemBase {
  equipmentType: EquipmentType;
  attributeModifiers: AttributeModifier[];
  itemBudget: number;
  itemBudgetTier: number;
}

export interface EssenceItem extends ItemBase {
  essence?: EssenceDefinitionDto;
  essenceDefinitionId: string;
  dismantleDustAmount: number;
}

export function essenceItemToEssence(item: EssenceItem): Essence {
  const essenceDefinitionId = inferEssenceDefinitionId(item);
  return {
    id: essenceDefinitionId,
    name: item.name,
    active: {
      name: 'Unbound Essence',
      description: item.description,
      tags: [],
      attackTypes: [],
      damageTypes: [],
      effectTags: [],
      targets: [],
      cooldown: 0,
      threatValue: 0,
      threatMultiplier: 1,
      effects: [],
    },
    passive: {
      name: 'Soul Archive',
      description: 'Absorb this item to add it to the Soul Archive.',
      tags: [],
      attackTypes: [],
      damageTypes: [],
      effectTags: [],
      targets: [],
      cooldown: 0,
      threatValue: 0,
      threatMultiplier: 1,
      effects: [],
    },
    attributeModifiers: [],
  };
}

export function inferEssenceDefinitionId(item: EssenceItem): string {
  return item.essenceDefinitionId || item.id.replace(/^item\./i, '');
}
