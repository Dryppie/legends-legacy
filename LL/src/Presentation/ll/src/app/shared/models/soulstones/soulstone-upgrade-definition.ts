import { CostCurve } from './cost-curve';
import { SoulstoneUpgradeType } from './soulstone-upgrade-type';
import { UpgradeEffect } from './upgrade-effects';

export interface SoulstoneUpgradeDefinition {
  id: string;
  name: string;
  maxLevel: number;
  description: string;
  cost: CostCurve;
  effect: UpgradeEffect;
  type: SoulstoneUpgradeType;
}
