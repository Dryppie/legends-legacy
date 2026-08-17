export const ABILITY_TARGET_SELECTORS = [
  'Self',
  'CurrentTarget',
  'Source',
  'EventSource',
  'EventTarget',
  'RandomEnemy',
  'LowestHealthAlly',
  'AllEnemies',
  'AllAllies',
  'EveryoneButSelf',
  'TwoEnemies',
  'TwoAllies',
  'HighestMaxHealthAlly',
  'SummonedAllies',
  'NonSummonedAllies',
  'SummonedEnemies',
  'LowestHealthEnemy',
  'HighestHealthEnemy',
  'LowestCurrentHealthEnemy',
  'HighestMaxHealthEnemy',
  'HighestCurrentHealthOwnedSummon',
  'OwnedSummons',
  'RandomAlly',
  'TwoRandomEnemies',
  'ThreeRandomEnemies',
  'ThreeEnemies',
] as const;

export type AbilityTargetSelector = (typeof ABILITY_TARGET_SELECTORS)[number];

const abilityTargetSelectorSet = new Set<string>(ABILITY_TARGET_SELECTORS);

export function isAbilityTargetSelector(
  value: string,
): value is AbilityTargetSelector {
  return abilityTargetSelectorSet.has(value);
}
