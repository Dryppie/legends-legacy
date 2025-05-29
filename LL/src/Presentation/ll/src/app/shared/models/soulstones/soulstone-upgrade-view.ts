import { SoulstoneUpgradeDefinition } from './soulstone-upgrade-definition';

export interface SoulstoneUpgradeView {
  definition: SoulstoneUpgradeDefinition;
  level: number;
  nextCost?: number;
}
