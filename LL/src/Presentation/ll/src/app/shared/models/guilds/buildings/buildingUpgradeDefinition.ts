import { BuildingCostCurve } from './buildingCostCurve';
import { BuildingEffect } from './buildingEffect';
import { BuildingType } from './buildingType';

export interface BuildingUpgradeDefinition {
  id: string;
  name: string;
  maxLevel: number;
  description: string;
  costCurves: BuildingCostCurve[];
  effect: BuildingEffect;
  type: BuildingType;
}
