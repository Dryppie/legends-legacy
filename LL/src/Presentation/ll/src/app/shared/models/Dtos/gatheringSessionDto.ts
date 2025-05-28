import { InventoryItem } from '../inventoryItem';
import { ProfessionType } from './characterProfession';

export interface GatheringSessionDto {
  from: Date;
  to: Date;

  gatheringSummary: GatheringSummary;
}

export interface GatheringSummary {
  professionType: ProfessionType;
  loot: InventoryItem[];
  totalActions: number;
  totalExperience: number;
}
