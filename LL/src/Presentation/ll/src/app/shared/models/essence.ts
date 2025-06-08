import { AttributeModifier } from './Dtos/attributesDto';
import { EffectType } from './enums/effectType';
import { ResourceType } from './enums/resourceType';
import { Targeting } from './enums/targeting';

export interface Essence {
  id: string;
  name: string;
  active: Ability;
  passive: Ability;
  attributeModifiers: AttributeModifier[];
}

export interface Ability {
  name: string;
  effectTypes: EffectType[];
  description: string;
  attackType?: AttackType;
  damageType?: DamageType;
  damageTags?: DamageTag[];
  targeting: Targeting[];
  cooldown: number;
  cost: number;
  costType: ResourceType;
}

export enum AttackType {
  Melee = 'Melee',
  Ranged = 'Ranged',
  DamageOverTime = 'DamageOverTime',
}

export enum DamageType {
  Physical = 'Physical',
  Magical = 'Magical',
  Bleed = 'Bleed',
  Burn = 'Burn',
  Poison = 'Poison',
}

export enum DamageTag {
  Slashing = 'Slashing',
  Blunt = 'Blunt',
  Piercing = 'Piercing',
  Arrows = 'Arrows',
  Spells = 'Spells',
}
