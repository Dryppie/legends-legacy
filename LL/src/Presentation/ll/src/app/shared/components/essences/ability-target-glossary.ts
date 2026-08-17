import { AbilityTargetSelector } from '../../models/enums/targeting';

export interface AbilityTargetDefinition {
  selector: AbilityTargetSelector;
  label: string;
  description: string;
  aliases: readonly string[];
}

export const ABILITY_TARGETS: readonly AbilityTargetDefinition[] = [
  {
    selector: 'Self',
    label: 'Self',
    description: 'The entity using or triggering the ability.',
    aliases: ['yourself'],
  },
  {
    selector: 'CurrentTarget',
    label: 'Current target',
    description:
      'An enemy selected using threat. Taunts can lock this enemy as the current target.',
    aliases: ['current target'],
  },
  {
    selector: 'Source',
    label: 'Ability source',
    description: 'The entity using or triggering the ability.',
    aliases: ['ability source'],
  },
  {
    selector: 'EventSource',
    label: 'Trigger source',
    description:
      'The entity that caused the triggering event, when that event has a source.',
    aliases: ['trigger source', 'event source'],
  },
  {
    selector: 'EventTarget',
    label: 'Trigger target',
    description:
      'The entity affected by the triggering event, when that event has a target.',
    aliases: ['trigger target', 'event target'],
  },
  {
    selector: 'RandomEnemy',
    label: 'Random enemy',
    description: 'One enemy chosen at random.',
    aliases: ['a random enemy', 'random enemy'],
  },
  {
    selector: 'LowestHealthAlly',
    label: 'Lowest-Health ally',
    description:
      'The ally with the lowest current Health amount, including the user and allied summons.',
    aliases: ['lowest-health ally', 'ally with the lowest health'],
  },
  {
    selector: 'AllEnemies',
    label: 'All enemies',
    description: 'All enemies, including summons.',
    aliases: ['all enemies'],
  },
  {
    selector: 'AllAllies',
    label: 'All allies',
    description: 'Every ally, including the user and allied summons.',
    aliases: ['all allies'],
  },
  {
    selector: 'EveryoneButSelf',
    label: 'Everyone except self',
    description:
      'Every other entity, including both allies and enemies, but not the ability user.',
    aliases: ['everyone except yourself', 'everyone but yourself'],
  },
  {
    selector: 'TwoEnemies',
    label: 'Two enemies',
    description: 'Up to two enemies.',
    aliases: ['two enemies'],
  },
  {
    selector: 'TwoAllies',
    label: 'Two allies',
    description: 'Up to two allies, including the user and allied summons.',
    aliases: ['two allies'],
  },
  {
    selector: 'HighestMaxHealthAlly',
    label: 'Highest-Max-Health ally',
    description:
      'The ally with the highest Max Health, including the user and allied summons.',
    aliases: ['highest-max-health ally', 'ally with the highest max health'],
  },
  {
    selector: 'SummonedAllies',
    label: 'Summoned allies',
    description: 'Every summoned ally.',
    aliases: ['summoned allies', 'allied summons'],
  },
  {
    selector: 'NonSummonedAllies',
    label: 'Non-summoned allies',
    description: 'Every non-summoned ally, including the user.',
    aliases: ['non-summoned allies'],
  },
  {
    selector: 'SummonedEnemies',
    label: 'Summoned enemies',
    description: 'Every summoned entity on the opposing team.',
    aliases: ['summoned enemies', 'enemy summons'],
  },
  {
    selector: 'LowestHealthEnemy',
    label: 'Lowest-Health-% enemy',
    description:
      'The enemy with the lowest percentage of Health remaining, not necessarily the lowest Health amount.',
    aliases: [
      'enemy with the lowest health percentage',
      'lowest-health-percentage enemy',
    ],
  },
  {
    selector: 'HighestHealthEnemy',
    label: 'Highest-current-Health enemy',
    description: 'The enemy with the highest current Health amount.',
    aliases: ['enemy with the highest health', 'highest-health enemy'],
  },
  {
    selector: 'LowestCurrentHealthEnemy',
    label: 'Lowest-current-Health enemy',
    description: 'The enemy with the lowest current Health amount.',
    aliases: [
      'enemy with the lowest current health',
      'lowest-current-health enemy',
    ],
  },
  {
    selector: 'HighestMaxHealthEnemy',
    label: 'Highest-Max-Health enemy',
    description: 'The enemy with the highest Max Health.',
    aliases: ['enemy with the highest max health', 'highest-max-health enemy'],
  },
  {
    selector: 'HighestCurrentHealthOwnedSummon',
    label: 'Healthiest owned summon',
    description:
      "The user's summon with the highest current Health, but only if that summon is healthier than the user.",
    aliases: ['healthiest owned summon'],
  },
  {
    selector: 'OwnedSummons',
    label: 'Owned summons',
    description: 'Every summon of the required type that belongs to the user.',
    aliases: ['owned summons', 'your summons'],
  },
  {
    selector: 'RandomAlly',
    label: 'Random ally',
    description: 'One random ally, including the user and allied summons.',
    aliases: ['a random ally', 'random ally'],
  },
  {
    selector: 'TwoRandomEnemies',
    label: 'Two random enemies',
    description: 'Up to two distinct living enemies chosen at random.',
    aliases: ['two random enemies'],
  },
  {
    selector: 'ThreeRandomEnemies',
    label: 'Three random enemies',
    description: 'Up to three enemies chosen at random.',
    aliases: ['three random enemies'],
  },
  {
    selector: 'ThreeEnemies',
    label: 'Three enemies',
    description:
      'Up to three enemies in combat order. The targets are not chosen randomly.',
    aliases: ['three enemies', 'up to three enemies'],
  },
];

export const ABILITY_TARGET_BY_SELECTOR = new Map(
  ABILITY_TARGETS.map((definition) => [definition.selector, definition]),
);
