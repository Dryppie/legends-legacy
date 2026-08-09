export type CombatKeywordValueMeaning =
  | 'seconds'
  | 'potency'
  | 'stacks'
  | 'charges'
  | 'percent'
  | 'none';

export interface CombatKeywordDefinition {
  name: string;
  aliases?: string[];
  description: string;
  descriptionWithValue?: string;
  valueMeaning?: CombatKeywordValueMeaning;
}

export const COMBAT_KEYWORDS: readonly CombatKeywordDefinition[] = [
  {
    name: 'Haste',
    aliases: ['Hasted'],
    description: 'Increases Basic Attack rate by 25% for 10 seconds.',
  },
  {
    name: 'Slow',
    aliases: ['Slowed'],
    description: 'Reduces Basic Attack rate by 25% for 10 seconds.',
  },
  {
    name: 'Empower',
    aliases: ['Empowered'],
    description: 'Increases effective Power by 20% for 10 seconds.',
  },
  {
    name: 'Weaken',
    aliases: ['Weakened'],
    description: 'Reduces effective Power by 20% for 10 seconds.',
  },
  {
    name: 'Vulnerable',
    description:
      'The next direct hit deals 25% increased damage and consumes one stack. It lasts until triggered or removed.',
    valueMeaning: 'stacks',
  },
  {
    name: 'Wound',
    aliases: ['Wounded'],
    description: 'Reduces healing received by 30%.',
    valueMeaning: 'seconds',
  },
  {
    name: 'Recovery',
    aliases: ['Recovering'],
    description: 'Increases healing received by 30%.',
    valueMeaning: 'seconds',
  },
  {
    name: 'Decay',
    aliases: ['Decaying'],
    description: 'Reduces Health Regeneration received by 30%.',
    valueMeaning: 'seconds',
  },
  {
    name: 'Renewal',
    aliases: ['Renewed'],
    description: 'Increases Health Regeneration received by 30%.',
    valueMeaning: 'seconds',
  },
  {
    name: 'Guard',
    aliases: ['Guarded'],
    description:
      'Each charge reduces the damage received of one direct hit by 25%.',
    valueMeaning: 'charges',
  },
  {
    name: 'Ward',
    aliases: ['Warded'],
    description:
      'Each charge prevents one harmful condition or harmful status effect.',
    valueMeaning: 'charges',
  },
  {
    name: 'Unstoppable',
    description: 'Prevents Stun, Freeze, and control-tagged status effects.',
    valueMeaning: 'seconds',
  },
  {
    name: 'Poison',
    aliases: ['Poisoned'],
    description:
      'Poison deals damage based on your Power every 2 seconds for 12 seconds.',
    descriptionWithValue:
      'Poison({value}) deals {value}% of your Power every 2 seconds for 12 seconds.',
    valueMeaning: 'potency',
  },
  {
    name: 'Burn',
    aliases: ['Burning'],
    description:
      'Burn deals damage based on your Power every second for 4 seconds.',
    descriptionWithValue:
      'Burn({value}) deals {value}% of your Power every second for 4 seconds.',
    valueMeaning: 'potency',
  },
  {
    name: 'Bleed',
    aliases: ['Bleeding'],
    description:
      'Bleed deals damage based on your Power every 2 seconds for 8 seconds.',
    descriptionWithValue:
      'Bleed({value}) deals {value}% of your Power every 2 seconds for 8 seconds.',
    valueMeaning: 'potency',
  },
  {
    name: 'Stun',
    aliases: ['Stunned'],
    description:
      'Prevents all actions. Standard Stun applications have an 80% success chance.',
    valueMeaning: 'seconds',
  },
  {
    name: 'Taunt',
    aliases: ['Taunted'],
    description:
      'Greatly increases threat, making the target more likely to attack you.',
    valueMeaning: 'seconds',
  },
  {
    name: 'Stealth',
    aliases: ['Stealthed'],
    description: 'Reduces effective threat to its minimum value.',
    valueMeaning: 'seconds',
  },
  {
    name: 'Chill',
    aliases: ['Chilled'],
    description:
      'Reduces Basic Attack rate by 1% per stack, up to 20 stacks, for 10 seconds.',
    valueMeaning: 'stacks',
  },
  {
    name: 'Freeze',
    aliases: ['Frozen'],
    description:
      'Prevents all actions. Standard Freeze applications have an 80% success chance.',
    valueMeaning: 'seconds',
  },
  {
    name: 'Corrosion',
    aliases: ['Corroded'],
    description:
      'Reduces Armor and Resistance by 1% per stack, up to 50 stacks, for 12 seconds.',
    valueMeaning: 'stacks',
  },
  {
    name: 'Doom',
    aliases: ['Doomed'],
    description:
      "After 15 seconds, deals Magical Damage equal to the listed percentage of the applier's snapshotted Power.",
    valueMeaning: 'percent',
  },
  {
    name: 'Thorns',
    description:
      'Reflects the listed percentage of Health damage taken back to the attacker.',
    valueMeaning: 'percent',
  },
  {
    name: 'Barrier',
    description: 'Absorbs incoming damage before Health is lost.',
  },
  {
    name: 'Critical Chance',
    description: 'The chance for a direct hit to deal Critical Damage.',
  },
  {
    name: 'Critical Hit',
    aliases: ['critically hit'],
    description: "A direct hit amplified by the attacker's Critical Damage.",
  },
  {
    name: 'Basic Attack',
    aliases: ['Basic Attacks'],
    description: 'Your basic attack uses your weapon(s).',
  },
  {
    name: 'Direct Hit',
    description:
      'Damage dealt immediately, rather than over time or by reflection.',
  },
  {
    name: 'Damage over Time',
    description:
      'Damage applied in periodic ticks instead of a single direct hit.',
  },
  {
    name: 'Foxfire Stack',
    aliases: ['Foxfire stacks'],
    description:
      'A stored retaliation charge consumed when its owner is attacked.',
    valueMeaning: 'stacks',
  },
  {
    name: 'Toxic Blood',
    description: 'Applies Poison(1) every second for 10 seconds.',
  },
  {
    name: 'Curse',
    description: 'Deals Magical curse damage over time.',
  },
  {
    name: 'Shadow Image',
    description: 'A short-lived marker used by a shadow image.',
  },
  {
    name: 'Evasive Shift',
    description: 'Increases Dodge chance for a short duration.',
  },
  {
    name: 'Distracted',
    description: 'Reduces Crit Chance through illusion.',
  },
  {
    name: 'Spirit Blight',
    description: 'Reduces Healing Power for a short duration.',
  },
];
