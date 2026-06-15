import { Injectable } from '@angular/core';
import { catchError, map, Observable, throwError } from 'rxjs';
import { MarketPlaceListing } from '../../../../shared/models/Dtos/market-place/market-place-listing';
import { ApiService } from '../api.service';
import { CreateMarketPlaceListingRequest } from '../../../../shared/models/requestDtos/market-place/create-market-place-listing-request';
import { BuyoutMarketPlaceListingRequest } from '../../../../shared/models/requestDtos/market-place/buyout-market.place-listing-request';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { InventoryItem } from '../../../../shared/models/inventoryItem';

export interface BuyoutMarketPlaceListingResponse {
  listingId: string;
  remainingListing: MarketPlaceListing | null;
  purchasedItem: InventoryItem;
  buyerCinders: number;
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

  createListing(
    listing: CreateMarketPlaceListingRequest,
  ): Observable<MarketPlaceListing> {
    return this.api.post('marketplace/createListing', listing).pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to create listing'));
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

  cancelListing(listingId: string): Observable<boolean> {
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
}
