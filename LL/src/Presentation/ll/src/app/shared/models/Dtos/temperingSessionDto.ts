export interface TemperingSessionDto {
  from: Date;
  to: Date;

  temperingSummary: TemperingSummary;
}

export interface TemperingSummary {
  totalItemsCrafted: number;
  masterpieces: number;
  levelingItems: number;
  qualityIncreases: number;
  totalActions: number;
  totalSoulstones: number;
  craftingExperience: number;
  totalExperience: number;
}
