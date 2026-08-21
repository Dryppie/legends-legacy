import { MarketPlaceBuyOrder } from './market-place-buy-order';
import { MarketPlaceListing } from './market-place-listing';
import { MarketPlaceOrder } from './market-place-order';

export interface MarketplaceListingChange {
  listingId: string;
  listing: MarketPlaceListing | null;
}

export interface MarketplaceBuyOrderChange {
  buyOrderId: string;
  buyOrder: MarketPlaceBuyOrder | null;
}

export interface MarketplaceChangeSet {
  version: number;
  listingChanges: MarketplaceListingChange[];
  buyOrderChanges: MarketplaceBuyOrderChange[];
  orders: MarketPlaceOrder[];
  affectedCharacterIds: string[];
}
