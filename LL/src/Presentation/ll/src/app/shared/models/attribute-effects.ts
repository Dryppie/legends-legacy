import { AttributeType } from './enums/attributeType';

interface Linear {
  kind: 'linear';
  coefficient: number;
} // +coef × every point
interface Step {
  kind: 'step';
  every: number;
  amount: number;
} // +amount every N points
export type Effect = (Linear | Step) & { gives: AttributeType }; // <- secondary stat

export const ATTRIBUTE_EFFECTS: Partial<Record<AttributeType, Effect[]>> = {
  [AttributeType.Constitution]: [
    { gives: AttributeType.Health, kind: 'linear', coefficient: 8 },
    {
      gives: AttributeType.HealthRegeneration,
      kind: 'step',
      every: 20,
      amount: 1,
    },
    {
      gives: AttributeType.CrowdControlResistance,
      kind: 'step',
      every: 12,
      amount: 1,
    },
  ],

  [AttributeType.Endurance]: [
    { gives: AttributeType.Health, kind: 'linear', coefficient: 2 },
    { gives: AttributeType.PhysicalDefense, kind: 'step', every: 4, amount: 3 },
    {
      gives: AttributeType.CritDamageReduction,
      kind: 'step',
      every: 10,
      amount: 0.5,
    },
  ],

  [AttributeType.Willpower]: [
    {
      gives: AttributeType.ManaRegeneration,
      kind: 'step',
      every: 20,
      amount: 1,
    },
    { gives: AttributeType.MagicalDefense, kind: 'step', every: 4, amount: 3 },
    {
      gives: AttributeType.CritDamageReduction,
      kind: 'step',
      every: 10,
      amount: 0.5,
    },
  ],

  [AttributeType.Strength]: [
    { gives: AttributeType.CritDamage, kind: 'step', every: 10, amount: 1 },
    { gives: AttributeType.Block, kind: 'linear', coefficient: 1 },
  ],

  [AttributeType.FightingSpirit]: [
    {
      gives: AttributeType.HealthRegeneration,
      kind: 'step',
      every: 50,
      amount: 1,
    },
    {
      gives: AttributeType.CrowdControlResistance,
      kind: 'step',
      every: 20,
      amount: 1,
    },
    { gives: AttributeType.Parry, kind: 'step', every: 4, amount: 1 },
  ],

  [AttributeType.Dexterity]: [
    { gives: AttributeType.CritChance, kind: 'step', every: 5, amount: 0.4 },
    { gives: AttributeType.Parry, kind: 'step', every: 4, amount: 1 },
  ],

  [AttributeType.Agility]: [
    { gives: AttributeType.Dodge, kind: 'step', every: 10, amount: 1 },
    {
      gives: AttributeType.BasicAttackSpeed,
      kind: 'step',
      every: 25,
      amount: 1,
    },
  ],

  [AttributeType.Intelligence]: [
    { gives: AttributeType.Mana, kind: 'linear', coefficient: 2 },
    { gives: AttributeType.CritDamage, kind: 'step', every: 10, amount: 1 },
  ],

  [AttributeType.Wisdom]: [
    { gives: AttributeType.Mana, kind: 'linear', coefficient: 1 },
    {
      gives: AttributeType.ManaRegeneration,
      kind: 'step',
      every: 50,
      amount: 1,
    },
  ],

  [AttributeType.Instinct]: [
    { gives: AttributeType.Dodge, kind: 'step', every: 10, amount: 1 },
    { gives: AttributeType.Parry, kind: 'step', every: 2, amount: 1 },
  ],

  [AttributeType.Perception]: [
    { gives: AttributeType.CritChance, kind: 'step', every: 3, amount: 0.1 },
    { gives: AttributeType.CritDamage, kind: 'step', every: 10, amount: 1 },
  ],

  [AttributeType.Luck]: [
    { gives: AttributeType.CritChance, kind: 'step', every: 3, amount: 0.2 },
  ],
};
