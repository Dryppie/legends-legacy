import { BuildingUpgradeDefinition } from './buildingUpgradeDefinition';

export interface BuildingUpgradeView {
  definition: BuildingUpgradeDefinition;
  level: number;
  nextCost?: Record<string, number>; // e.g. { cinders: 2500, soulstones: 50 }
}
