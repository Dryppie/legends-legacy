import { ItemBase } from '../../item';

export interface MarketPlaceOrder {
  id: string;
  sellerId: string;
  buyerId: string;
  itemBaseId: string;
  itemBase: ItemBase;
  itemInstanceId: string | null;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  sellerFee: number;
  source: number | string;
  purchasedAt: Date;
}
