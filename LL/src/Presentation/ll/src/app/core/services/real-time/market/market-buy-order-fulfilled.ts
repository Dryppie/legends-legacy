import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { MarketPlaceBuyOrder } from '../../../../shared/models/Dtos/market-place/market-place-buy-order';

export interface MarketBuyOrderFulfilledMsg {
  buyOrderId: string;
  buyerId: string;
  sellerId: string;
  quantity: number;
  totalPrice: number;
  sellerCinders: number;
  purchasedItem: InventoryItem;
  remainingBuyOrder: MarketPlaceBuyOrder | null;
}
