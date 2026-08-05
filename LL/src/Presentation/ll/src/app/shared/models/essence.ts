import { AttributeModifier } from './Dtos/attributesDto';
import { EssenceEffectDto } from './essence-system';
import { EffectTag } from './enums/effectType';
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
  description: string;
  attackTypes: AttackType[];
  damageTypes: DamageType[];
  effectTags: EffectTag[];
  targeting: Targeting[];
  cooldown: number;
  effects: EssenceEffectDto[];
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
