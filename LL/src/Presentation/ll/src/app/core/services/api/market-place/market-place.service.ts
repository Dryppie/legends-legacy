import { Injectable } from '@angular/core';
import { catchError, map, Observable, throwError } from 'rxjs';
import { MarketPlaceListing } from '../../../../shared/models/Dtos/market-place/market-place-listing';
import { ApiService } from '../api.service';
import { CreateMarketPlaceListingRequest } from '../../../../shared/models/requestDtos/market-place/create-market-place-listing-request';

@Injectable({
  providedIn: 'root',
})
export class MarketPlaceService {
  constructor(private readonly api: ApiService) {}

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
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error('Failed to get inventory'));
      }),
    );
  }

  createListing(listing: CreateMarketPlaceListingRequest): Observable<boolean> {
    return this.api.post('marketplace/createListing', listing).pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get inventory'));
      }),
    );
  }
}
