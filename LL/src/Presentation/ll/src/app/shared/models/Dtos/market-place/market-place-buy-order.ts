import { ItemBase } from '../../item';

export interface MarketPlaceBuyOrder {
  id: string;
  buyerId: string;
  buyerName: string;
  itemBaseId: string;
  itemBase: ItemBase;
  quantity: number;
  unitPrice: number;
  createdAt: Date;
}
