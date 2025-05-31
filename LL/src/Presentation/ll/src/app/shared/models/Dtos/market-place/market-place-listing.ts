import { ItemInstance } from '../../item';

export interface MarketPlaceListing {
  id: string;
  itemInstanceId: string;
  itemInstance: ItemInstance;
  quantity: number;
  unitPrice: number;
  createdAt: Date;
}
