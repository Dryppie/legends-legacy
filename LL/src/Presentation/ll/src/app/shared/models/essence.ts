export interface Essence {
  name: string;
  activeAbility: Ability;
  passiveAbility: Ability;
}

export interface Ability {
  name: string;
  attackType: AttackType;
  damageType: DamageType;
  damageTags: DamageTag[];
  targeting: Targeting[];
  cooldown: number;
  cost: number;
}

export enum AttackType {
  Melee,
  Ranged,
  DamageOverTime,
}

export enum DamageType {
  Physical,
  Magical,
  Bleed,
  Burn,
  Poison,
}

export enum DamageTag {
  Slashing,
  Blunt,
  Piercing,
  Arrows,
  Spells,
}

export enum Targeting {
  None,
  Self,
  SingleEnemy,
  SingleAlly,
  TwoEnemies,
  TwoAllies,
  SingleDeadEnemy,
  SingleDeadAlly,
  SingleRandomEnemy,
  SingleRandomAlly,
  SingleEnemyLowestHealth,
  SingleAllyLowestHealth,
  AllEnemies,
  AllAllies,
  AllAlliesAndSelf,
}
