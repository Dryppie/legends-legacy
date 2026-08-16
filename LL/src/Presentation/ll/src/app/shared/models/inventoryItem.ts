import { ItemInstance } from './item';

export interface InventoryItem {
  id: string;
  itemInstance: ItemInstance;
  quantity: number;
  /** A crafted item the character has not inspected yet. Server-owned. */
  isNew?: boolean;
}
