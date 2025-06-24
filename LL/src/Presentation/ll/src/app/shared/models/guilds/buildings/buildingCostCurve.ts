export interface BuildingCostCurve {
  resource: string;
  base: number;
  increment: number;
  incrementCap?: number;
}
