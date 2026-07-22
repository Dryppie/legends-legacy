export type PowerAnalysisState =
  | 'Available'
  | 'Unsupported'
  | 'InsufficientCombatData'
  | 'LowConfidence'
  | 'CalculationFailed';

export interface OverallPowerRating {
  overall: number;
  state: PowerAnalysisState;
}
