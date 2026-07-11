import { Injectable } from '@angular/core';
import { catchError, map, Observable, throwError } from 'rxjs';
import { MarketPlaceListing } from '../../../../shared/models/Dtos/market-place/market-place-listing';
import { MarketPlaceBuyOrder } from '../../../../shared/models/Dtos/market-place/market-place-buy-order';
import { ApiService } from '../api.service';
import { CreateMarketPlaceListingRequest } from '../../../../shared/models/requestDtos/market-place/create-market-place-listing-request';
import { CreateMarketPlaceBuyOrderRequest } from '../../../../shared/models/requestDtos/market-place/create-market-place-buy-order-request';
import { BuyoutMarketPlaceListingRequest } from '../../../../shared/models/requestDtos/market-place/buyout-market.place-listing-request';
import { FulfillMarketPlaceBuyOrderRequest } from '../../../../shared/models/requestDtos/market-place/fulfill-market-place-buy-order-request';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { InventoryItem } from '../../../../shared/models/inventoryItem';

export interface BuyoutMarketPlaceListingResponse {
  listingId: string;
  remainingListing: MarketPlaceListing | null;
  purchasedItem: InventoryItem;
  buyerCinders: number;
}

export interface CreateMarketPlaceListingResponse {
  listing: MarketPlaceListing;
  listedItemInstanceId: string;
  listedQuantity: number;
  remainingInventoryItem: InventoryItem | null;
}

export interface CreateMarketPlaceBuyOrderResponse {
  buyOrder: MarketPlaceBuyOrder;
  buyerCinders: number;
}

export interface FulfillMarketPlaceBuyOrderResponse {
  buyOrderId: string;
  remainingBuyOrder: MarketPlaceBuyOrder | null;
  purchasedItem: InventoryItem;
  remainingSellerInventoryItem: InventoryItem | null;
  soldItemInstanceId: string;
  soldQuantity: number;
  sellerCinders: number;
}

export interface CancelMarketPlaceBuyOrderResponse {
  buyOrderId: string;
  buyerCinders: number;
}

export interface CancelMarketPlaceListingResponse {
  listingId: string;
  returnedItem: InventoryItem;
}

@Injectable({
  providedIn: 'root',
})
export class MarketPlaceService {
  constructor(
    private readonly api: ApiService,
    private toast: ToastService,
  ) {}

  getListings(): Observable<MarketPlaceListing[]> {
    return this.api.get('marketplace').pipe(
      map((marketplaceListings) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
        return marketplaceListings;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to get inventory'));
      }),
    );
  }

  getBuyOrders(): Observable<MarketPlaceBuyOrder[]> {
    return this.api.get('marketplace/buyOrders').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get buy orders'));
      }),
    );
  }

  createListing(
    listing: CreateMarketPlaceListingRequest,
  ): Observable<CreateMarketPlaceListingResponse> {
    return this.api.post('marketplace/createListing', listing).pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to create listing'));
      }),
    );
  }

  createBuyOrder(
    buyOrder: CreateMarketPlaceBuyOrderRequest,
  ): Observable<CreateMarketPlaceBuyOrderResponse> {
    return this.api.post('marketplace/createBuyOrder', buyOrder).pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to create buy order'));
      }),
    );
  }

  buyoutListing(
    listing: BuyoutMarketPlaceListingRequest,
  ): Observable<BuyoutMarketPlaceListingResponse> {
    return this.api.post('marketplace/buyoutListing', listing).pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to buy listing'));
      }),
    );
  }

  fulfillBuyOrder(
    fulfillment: FulfillMarketPlaceBuyOrderRequest,
  ): Observable<FulfillMarketPlaceBuyOrderResponse> {
    return this.api.post('marketplace/fulfillBuyOrder', fulfillment).pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to fulfill buy order'));
      }),
    );
  }

  cancelListing(listingId: string): Observable<CancelMarketPlaceListingResponse> {
    return this.api.post('marketplace/cancelListing', listingId).pipe(
      catchError(() => {
        this.toast.showToast(
          'Order cancellation failed',
          'Order might have been purchased.',
          false,
          't',
        );
        return throwError(() => new Error('Failed to cancel listing'));
      }),
    );
  }

  cancelBuyOrder(
    buyOrderId: string,
  ): Observable<CancelMarketPlaceBuyOrderResponse> {
    return this.api.post('marketplace/cancelBuyOrder', buyOrderId).pipe(
      catchError(() => {
        this.toast.showToast(
          'Buy order cancellation failed',
          'Order might have already been filled.',
          false,
          't',
        );
        return throwError(() => new Error('Failed to cancel buy order'));
      }),
    );
  }
}
