import { InventoryItem } from '../../../../shared/models/inventoryItem';

export interface LootReceivedMsg {
  payload: InventoryItem[]; // same DTO you already fetch via REST
  source?: string;
  location?: string | null;
  grantId?: string | null;
}
