import {
  signal,
  computed,
  effect,
  Injectable,
  Signal,
  untracked,
} from '@angular/core';
import { finalize, tap } from 'rxjs/operators';

import {
  BuyoutMarketPlaceListingResponse,
  MarketPlaceService,
} from './market-place.service';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { MarketPlaceListing } from '../../../../shared/models/Dtos/market-place/market-place-listing';
import { CreateMarketPlaceListingRequest } from '../../../../shared/models/requestDtos/market-place/create-market-place-listing-request';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { Observable } from 'rxjs';
import { BuyoutMarketPlaceListingRequest } from '../../../../shared/models/requestDtos/market-place/buyout-market.place-listing-request';
import { CharacterService } from '../character/character.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { GameEventService } from '../../real-time/game-event.service';
import { MarketListingSoldMsg } from '../../real-time/market/market-listing-sold';
import { MarketListingCreatedMsg } from '../../real-time/market/market-listing-created';
import { MarketListingCanceledMsg } from '../../real-time/market/market-listing-canceled';

@Injectable({ providedIn: 'root' })
export class MarketplaceStateService {
  private readonly _listings = signal<MarketPlaceListing[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  private readonly myCharacterId!: Signal<string | null>;
  private lastMarketListingSoldEvent: unknown;
  private lastMarketListingCreatedEvent: unknown;
  private lastMarketListingCanceledEvent: unknown;
  private hasLoaded = false;

  readonly listings = computed(() => {
    return this._listings();
  });
  readonly myListings = computed(() =>
    this._listings().filter(
      (l) => l.sellerId === this.myCharacterId(), // adjust property name
    ),
  );
  readonly loading = computed(() => this._loading());
  readonly isEmpty = computed(() => this._listings().length === 0);
  readonly error = computed(() => this._error());

  constructor(
    private marketplaceService: MarketPlaceService,
    private characterService: CharacterService,
    private inventoryState: InventoryStateService,
    private eventService: GameEventService,
  ) {
    this.myCharacterId = computed(
      () => this.characterService.currentCharacterId(), // unwrap inside
    );

    effect(
      () => {
        const sale = this.eventService.event.MarketListingSoldMsg();
        const created = this.eventService.event.MarketListingCreatedMsg();
        const canceled = this.eventService.event.MarketListingCanceledMsg();

        if (sale && sale !== this.lastMarketListingSoldEvent) {
          this.lastMarketListingSoldEvent = sale;
          untracked(() => this.applySellerSale(sale));
        }

        if (created && created !== this.lastMarketListingCreatedEvent) {
          this.lastMarketListingCreatedEvent = created;
          untracked(() => this.applyCreatedListing(created));
        }

        if (canceled && canceled !== this.lastMarketListingCanceledEvent) {
          this.lastMarketListingCanceledEvent = canceled;
          untracked(() => this.applyCanceledListing(canceled));
        }
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const reconnectCount = this.eventService.reconnectCount();
        if (reconnectCount <= 0 || !this.hasLoaded) return;

        untracked(() => this.refresh());
      },
      { allowSignalWrites: true },
    );
  }

  byType = (type: ItemType) =>
    computed(
      () =>
        this._listings().filter(
          (l) => l.itemInstance.itemBase.itemType === type,
        ) ?? [],
    );

  readonly equipment = this.byType(ItemType.Equipment);
  readonly materials = this.byType(ItemType.Resource);
  readonly essences = this.byType(ItemType.Essence);

  load(): void {
    if (this._listings().length) return; // already cached
    this.refresh();
  }

  /** Force‑refresh from the backend, bypassing the in‑memory cache. */
  refresh(): void {
    this.hasLoaded = true;
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

  buyoutListing(
    listingId: string,
    quantity: number,
  ): Observable<BuyoutMarketPlaceListingResponse> {
    const listing: BuyoutMarketPlaceListingRequest = {
      marketPlaceListingId: listingId,
      quantity,
    };

    return this.marketplaceService.buyoutListing(listing).pipe(
      tap((response) => {
        this.applyBuyoutResponse(response);
      }),
    );
  }

  cancelListing(listingId: string): Observable<boolean> {
    return this.marketplaceService.cancelListing(listingId).pipe((success) => {
      this.removeListing(listingId);
      return success;
    });
  }

  createListing(
    item: InventoryItem,
    quantity: number,
    unitPrice: number,
  ): Observable<MarketPlaceListing> {
    const listing: CreateMarketPlaceListingRequest = {
      itemInstanceId: item.itemInstance.id,
      quantity,
      unitPrice,
    };

    // Uncomment once backend endpoint is ready
    return this.marketplaceService
      .createListing(listing)
      .pipe((createdListing) => createdListing);
  }

  addToListings(listing: MarketPlaceListing) {
    this.upsertListing(listing);
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

  private applyBuyoutResponse(response: BuyoutMarketPlaceListingResponse): void {
    this.applyListingChange(response.listingId, response.remainingListing);
    this.inventoryState.addOrIncrement(response.purchasedItem);
    this.updateCurrentCharacterCinders(response.buyerCinders);
  }

  private applySellerSale(sale: MarketListingSoldMsg): void {
    this.applyListingChange(sale.listingId, sale.remainingListing);

    if (sale.sellerId === this.myCharacterId()) {
      this.updateCurrentCharacterCinders(sale.sellerCinders);
    }
  }

  private applyCreatedListing(event: MarketListingCreatedMsg): void {
    this.upsertListing(event.listing);
  }

  private applyCanceledListing(event: MarketListingCanceledMsg): void {
    this.removeListing(event.listingId);
  }

  private applyListingChange(
    listingId: string,
    remainingListing: MarketPlaceListing | null,
  ): void {
    if (!remainingListing) {
      this.removeListing(listingId);
      return;
    }

    const listings = this._listings();
    const index = listings.findIndex((listing) => listing.id === listingId);
    if (index === -1) {
      this._listings.set([...listings, remainingListing]);
      return;
    }

    const updated = [...listings];
    updated[index] = remainingListing;
    this._listings.set(updated);
  }

  private updateCurrentCharacterCinders(cinders: number): void {
    const character = this.characterService.currentCharacter();
    if (!character) return;

    this.characterService.updateCharacter({
      ...character,
      cinders,
    });
  }

  private upsertListing(listing: MarketPlaceListing): void {
    const listings = this._listings();
    const index = listings.findIndex((current) => current.id === listing.id);
    if (index === -1) {
      this._listings.set([...listings, listing]);
      return;
    }

    const updated = [...listings];
    updated[index] = listing;
    this._listings.set(updated);
  }
}
