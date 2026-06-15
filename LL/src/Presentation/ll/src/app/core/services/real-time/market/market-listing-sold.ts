import { MarketPlaceListing } from '../../../../shared/models/Dtos/market-place/market-place-listing';

export interface MarketListingSoldMsg {
  listingId: string;
  sellerId: string;
  quantity: number;
  totalPrice: number;
  sellerCinders: number;
  remainingListing: MarketPlaceListing | null;
}
