import { AttributeModifier } from './Dtos/attributesDto';
import { AttributeType } from './enums/attributeType';
import { EquipmentType } from './enums/equipmentType';
import { GatheringType } from './enums/gatheringType';
import { ItemType } from './enums/itemType';
import { Rarity } from './enums/rarity';
import { Essence } from './essence';
import { EssenceDefinitionDto } from './essence-system';

export interface ItemInstance {
  id: string;
  itemBase: ItemBase;
  displayName?: string;
  source?: string;
  category?: string;
}

export interface EquipmentInstance extends ItemInstance {
  displayName: string;
  rarity: Rarity;
  equipmentBase: Equipment;
  potential?: number;
  itemXp: number;
  baseModifiers: AttributeModifier[];
  instanceModifiers: AttributeModifier[];
  attributeModifiers: AttributeModifier[];
  toolAffixes: ToolBonusModifier[];
  effectiveToolBonuses: ToolBonusModifier[];
}

export interface ItemBase {
  id: string;
  name: string;
  rarity: Rarity;
  itemType: ItemType;
  description: string;
  stackable: boolean;
  isBound?: boolean;
}

export interface Equipment extends ItemBase {
  equipmentType: EquipmentType;
  attributeModifiers: AttributeModifier[];
  toolBonuses?: ToolBonusModifier[];
  attackSpeed: number;
  magnitude: number;
  magnitudeRange: number;
  gatheringType?: GatheringType | null;
  scalingAttribute: AttributeType;
  scalingAmount: number;
}

export interface ToolBonusModifier {
  id: string;
  equipmentBaseId?: string;
  equipmentInstanceId?: string;
  name?: string;
  bonusType: ToolBonusType;
  amount: number;
  scopeId?: string;
}

export enum ToolBonusType {
  GatheringYieldPercent = 'GatheringYieldPercent',
  RareMaterialChancePercent = 'RareMaterialChancePercent',
  DoubleGatherChancePercent = 'DoubleGatherChancePercent',
  NodeSuccessChancePercent = 'NodeSuccessChancePercent',
  SpecificNodeYieldPercent = 'SpecificNodeYieldPercent',
  SpecificRegionYieldPercent = 'SpecificRegionYieldPercent',
  SpecificResourceYieldPercent = 'SpecificResourceYieldPercent',
  SpecificToolTypeYieldPercent = 'SpecificToolTypeYieldPercent',
  BonusRollChancePercent = 'BonusRollChancePercent',
  MinimumQuantityBonus = 'MinimumQuantityBonus',
  MaximumQuantityBonus = 'MaximumQuantityBonus',
}

export interface EssenceItem extends ItemBase {
  essence?: EssenceDefinitionDto;
  essenceDefinitionId: string;
  dismantleDustAmount: number;
}

export function essenceItemToEssence(item: EssenceItem): Essence {
  return {
    id: item.essenceDefinitionId || item.id,
    name: item.name,
    active: {
      name: 'Unbound Essence',
      description: item.description,
      attackTypes: [],
      damageTypes: [],
      effectTags: [],
      targeting: [],
      cooldown: 0,
      effects: [],
    },
    passive: {
      name: 'Soul Archive',
      description: 'Absorb this item to add it to the Soul Archive.',
      attackTypes: [],
      damageTypes: [],
      effectTags: [],
      targeting: [],
      cooldown: 0,
      effects: [],
    },
    attributeModifiers: [],
  };
}
