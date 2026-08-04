import { AttributeModifier } from './Dtos/attributesDto';
import { AttributeType } from './enums/attributeType';
import { EffectType } from './enums/effectType';
import { Targeting } from './enums/targeting';

export interface Essence {
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
  Shadow = 'Shadow',
}

export enum DamageTag {
  Slashing = 'Slashing',
  Blunt = 'Blunt',
  Piercing = 'Piercing',
  Arrows = 'Arrows',
  Spells = 'Spells',
}
