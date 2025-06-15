import { InventoryItem } from '../../../../shared/models/inventoryItem';

export interface LootReceivedMsg {
  loot: InventoryItem[]; // same DTO you already fetch via REST
}
