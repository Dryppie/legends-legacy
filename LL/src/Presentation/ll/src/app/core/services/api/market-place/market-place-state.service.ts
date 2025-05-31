import { signal, computed, Injectable } from '@angular/core';
import { finalize } from 'rxjs/operators';

import { MarketPlaceService } from './market-place.service';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { MarketPlaceListing } from '../../../../shared/models/Dtos/market-place/market-place-listing';
import { CreateMarketPlaceListingRequest } from '../../../../shared/models/requestDtos/market-place/create-market-place-listing-request';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class MarketplaceStateService {
  private readonly _listings = signal<MarketPlaceListing[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly listings = computed(() => this._listings());
  readonly loading = computed(() => this._loading());
  readonly isEmpty = computed(() => this._listings().length === 0);
  readonly error = computed(() => this._error());

  constructor(private marketplaceService: MarketPlaceService) {}

  byType = (type: ItemType) =>
    computed(
      () =>
        this._listings().filter(
          (l) => l.itemInstance.itemBase.itemType === type,
        ) ?? [],
    );

  readonly equipment = this.byType(ItemType.Equipment);
  readonly materials = this.byType(ItemType.Material);
  readonly essences = this.byType(ItemType.Essence);

  load(): void {
    if (this._listings().length) return; // already cached
    this.refresh();
  }

  /** Force‑refresh from the backend, bypassing the in‑memory cache. */
  refresh(): void {
    this._loading.set(true);
    this.marketplaceService
      .getListings()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (marketplaceListings) => {
          const sorted = marketplaceListings
            .slice() // defensive copy (optional)
            .sort((a, b) =>
              a.itemInstance.itemBase.itemType.localeCompare(
                b.itemInstance.itemBase.itemType,
              ),
            );

          this._listings.set(sorted);
        },
        error: (err) => this._error.set(err.message ?? 'Unknown error'),
      });
  }

  addListing(
    item: InventoryItem,
    quantity: number,
    unitPrice: number,
  ): Observable<boolean> {
    const listing: CreateMarketPlaceListingRequest = {
      itemInstanceId: item.itemInstance.id,
      quantity,
      unitPrice,
    };

    // Uncomment once backend endpoint is ready
    return this.marketplaceService
      .createListing(listing)
      .pipe((success) => success);
  }

  // Remove an existing listing.
  removeListing(listingId: string): void {
    const filtered = this._listings().filter((l) => l.id !== listingId);
    this._listings.set(filtered);
  }

  // Reduce the quantity of an existing stackable listing or remove it entirely when depleted.
  decrementListing(listingId: string, qty: number): void {
    const updated = this._listings()
      .map((l) => {
        if (l.id !== listingId) return l;

        if (l.itemInstance.itemBase.stackable) {
          const newQty = l.quantity - qty;
          if (newQty > 0) {
            return { ...l, quantity: newQty } as MarketPlaceListing;
          }
        }
        // new quantity is 0 or negative → remove listing
        return null;
      })
      .filter((l): l is MarketPlaceListing => l !== null);

    this._listings.set(updated);
  }
}
