export interface EssenceCatalogReport {
  regions: EssenceCatalogRegion[];
}

export interface EssenceCatalogRegion {
  id: string;
  name: string;
  areas: EssenceCatalogArea[];
}

export interface EssenceCatalogArea {
  id: string;
  name: string;
  sourceType: string;
  tier: string;
  monsters: EssenceCatalogMonster[];
}

export interface EssenceCatalogMonster {
  id: string;
  name: string;
  imagePath: string;
  sourceType: string;
  sourceName: string;
  tier: string;
  essence: EssenceCatalogEssence | null;
}

export interface EssenceCatalogEssence {
  id: string;
  name: string;
  description: string;
  rarity: string;
  itemId: string | null;
  tags: string[];
  attributeBonuses: EssenceCatalogAttributeBonus[];
  drop: EssenceCatalogDrop;
  activeAbility: EssenceCatalogAbility | null;
  passiveAbility: EssenceCatalogAbility | null;
}

export interface EssenceCatalogAttributeBonus {
  attribute: string;
  baseValue: number;
}

export interface EssenceCatalogDrop {
  baseDropChance: number;
  resonanceGainPerFailedEligibleKill: number;
  dropChanceBonusPerResonance: number;
  maxResonanceBonus: number;
}

export interface EssenceCatalogAbility {
  id: string;
  name: string;
  kind: string;
  description: string;
  cooldownTicks: number;
  tags: string[];
  triggers: EssenceCatalogTrigger[];
  effects: EssenceCatalogEffect[];
}

export interface EssenceCatalogTrigger {
  event: string;
  internalCooldownTicks: number;
  effectIds: string[];
  conditions: EssenceCatalogCondition[];
}

export interface EssenceCatalogEffect {
  id: string;
  operation: string;
  target: string;
  baseValue: number;
  scalingAttribute: string | null;
  scalingCoefficient: number;
  attribute: string | null;
  statusId: string | null;
  summonId: string | null;
  resource: string;
  durationTicks: number;
  intervalTicks: number;
  uses: number;
  attackType: string;
  damageType: string;
  lifeStealPercentage: number;
  tags: string[];
  conditions: EssenceCatalogCondition[];
}

export interface EssenceCatalogCondition {
  type: string;
  subject: string;
  statusId: string | null;
  tag: string | null;
  value: number;
}
