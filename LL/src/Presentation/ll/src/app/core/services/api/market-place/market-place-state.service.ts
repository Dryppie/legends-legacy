import {
  signal,
  computed,
  effect,
  Injectable,
  Signal,
  untracked,
} from '@angular/core';
import { finalize, map, tap } from 'rxjs/operators';

import {
  BuyoutMarketPlaceListingResponse,
  BuyCommodityResponse,
  SellCommodityResponse,
  CancelMarketPlaceBuyOrderResponse,
  CancelMarketPlaceListingResponse,
  CreateMarketPlaceBuyOrderResponse,
  CreateMarketPlaceListingResponse,
  FulfillMarketPlaceBuyOrderResponse,
  MarketPlaceService,
} from './market-place.service';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { MarketPlaceListing } from '../../../../shared/models/Dtos/market-place/market-place-listing';
import { MarketPlaceBuyOrder } from '../../../../shared/models/Dtos/market-place/market-place-buy-order';
import { CreateMarketPlaceListingRequest } from '../../../../shared/models/requestDtos/market-place/create-market-place-listing-request';
import { CreateMarketPlaceBuyOrderRequest } from '../../../../shared/models/requestDtos/market-place/create-market-place-buy-order-request';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { Observable } from 'rxjs';
import { BuyoutMarketPlaceListingRequest } from '../../../../shared/models/requestDtos/market-place/buyout-market.place-listing-request';
import { FulfillMarketPlaceBuyOrderRequest } from '../../../../shared/models/requestDtos/market-place/fulfill-market-place-buy-order-request';
import { CharacterService } from '../character/character.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { RealtimeSignalDeduper } from '../../real-time/game-realtime/realtime-deduplication';
import { ItemBase } from '../../../../shared/models/item';
import { MarketPlaceOrder } from '../../../../shared/models/Dtos/market-place/market-place-order';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { VersionedMutationResult } from '../api.service';
import { MarketplaceChangeSet } from '../../../../shared/models/Dtos/market-place/marketplace-change-set';

@Injectable({ providedIn: 'root' })
export class MarketplaceStateService {
  private readonly _listings = signal<MarketPlaceListing[]>([]);
  private readonly _buyOrders = signal<MarketPlaceBuyOrder[]>([]);
  private readonly _catalog = signal<ItemBase[]>([]);
  private readonly _history = signal<MarketPlaceOrder[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  private readonly myCharacterId!: Signal<string | null>;
  private readonly eventDeduper = new RealtimeSignalDeduper();
  private readonly hasLoaded = signal(false);
  private semanticGapRevision = 0;
  private refreshVersion = 0;
  private activeCharacterId: string | null | undefined;

  readonly listings = computed(() => this._listings());
  readonly buyOrders = computed(() => this._buyOrders());
  readonly catalog = computed(() => this._catalog());
  readonly history = computed(() => this._history());
  readonly myListings = computed(() =>
    this._listings().filter((l) => l.sellerId === this.myCharacterId()),
  );
  readonly myBuyOrders = computed(() =>
    this._buyOrders().filter((order) => order.buyerId === this.myCharacterId()),
  );
  readonly loading = computed(() => this._loading());
  readonly isEmpty = computed(
    () => this._listings().length === 0 && this._buyOrders().length === 0,
  );
  readonly error = computed(() => this._error());

  constructor(
    private marketplaceService: MarketPlaceService,
    private characterService: CharacterService,
    private inventoryState: InventoryStateService,
    private eventService: GameRealtimeEventRegistry,
    private toast: ToastService,
    private readonly stateSync: StateSyncCoordinator,
    private readonly domainVersions: DomainVersionTracker,
  ) {
    this.stateSync.register(
      'marketplace',
      'marketplace',
      () => this.synchronize(),
      () => this.hasLoaded(),
    );
    this.myCharacterId = computed(() =>
      this.characterService.currentCharacterId(),
    );

    effect(
      () => {
        const envelope = this.eventService.eventEnvelope.MarketplaceChanged();
        const event = envelope?.payload;
        if (
          event &&
          this.eventDeduper.shouldProcess('marketplace-changed', envelope)
        ) {
          untracked(() => this.applySemanticChanges(event.changes));
        }
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const characterId = this.myCharacterId();
        if (
          this.activeCharacterId !== undefined &&
          this.activeCharacterId !== characterId
        ) {
          untracked(() => this.resetForCharacterChange());
        }
        this.activeCharacterId = characterId;
      },
      { allowSignalWrites: true },
    );
  }

  private resetForCharacterChange(): void {
    this.refreshVersion += 1;
    this.semanticGapRevision = 0;
    this.hasLoaded.set(false);
    this.eventDeduper.clear();
    this._listings.set([]);
    this._buyOrders.set([]);
    this._catalog.set([]);
    this._history.set([]);
    this._loading.set(false);
    this._error.set(null);
  }

  byType = (type: ItemType) =>
    computed(
      () =>
        this._listings().filter(
          (l) => l.itemInstance.itemBase.itemType === type,
        ) ?? [],
    );

  buyOrdersByType = (type: ItemType) =>
    computed(
      () =>
        this._buyOrders().filter((order) => order.itemBase.itemType === type) ??
        [],
    );

  readonly equipment = this.byType(ItemType.Equipment);
  readonly materials = this.byType(ItemType.Resource);
  readonly essences = this.byType(ItemType.Essence);

  load(): void {
    if (this.hasLoaded()) return;
    this.refresh();
  }

  refresh(): void {
    this.synchronize().subscribe({ error: () => undefined });
  }

  private synchronize(): Observable<unknown> {
    this._loading.set(true);
    this._error.set(null);
    const refreshVersion = ++this.refreshVersion;

    return this.marketplaceService.getSnapshot().pipe(
      tap({
        next: ({ listings, catalog, history, buyOrders }) => {
          if (refreshVersion !== this.refreshVersion) return;

          this._listings.set(
            listings
              .slice()
              .sort((a, b) =>
                a.itemInstance.itemBase.itemType.localeCompare(
                  b.itemInstance.itemBase.itemType,
                ),
              ),
          );
          this._catalog.set(catalog);
          this._history.set(history);
          this._buyOrders.set(
            buyOrders
              .slice()
              .sort((a, b) =>
                a.itemBase.itemType.localeCompare(b.itemBase.itemType),
              ),
          );
          this.hasLoaded.set(true);
          this.stateSync.activate('marketplace', 'marketplace');
          this.semanticGapRevision = 0;
        },
        error: (err) => {
          if (refreshVersion === this.refreshVersion) {
            this._error.set(err.message ?? 'Unknown error');
          }
        },
      }),
      finalize(() => {
        if (refreshVersion === this.refreshVersion) {
          this._loading.set(false);
        }
      }),
    );
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
      tap((result) => {
        this.applyBuyoutResponse(result);
      }),
      map((result) => result.data),
    );
  }

  buyCommodity(
    itemBaseId: string,
    quantity: number,
    maximumUnitPrice: number,
  ): Observable<BuyCommodityResponse> {
    return this.marketplaceService
      .buyCommodity(itemBaseId, quantity, maximumUnitPrice)
      .pipe(
        tap((result) => {
          const response = result.data;
          this.applyVersionedMarketplace(result);
          this.applyVersionedCinders(result, response.buyerCinders);
          this.showTradeReceipt(
            'Bought',
            response.filledQuantity,
            response.totalPrice,
            this.itemName(itemBaseId),
          );
        }),
        map((result) => result.data),
      );
  }

  sellCommodity(
    itemInstanceId: string,
    quantity: number,
    minimumUnitPrice: number,
  ): Observable<SellCommodityResponse> {
    const itemName = this.inventoryState
      .items()
      .find((item) => item.itemInstance.id === itemInstanceId)?.itemInstance
      .itemBase.name;
    return this.marketplaceService
      .sellCommodity(itemInstanceId, quantity, minimumUnitPrice)
      .pipe(
        tap((result) => {
          const response = result.data;
          this.applyVersionedMarketplace(result);
          this.applyVersionedCinders(result, response.sellerCinders);
          this.inventoryState.applyVersionedInventoryDelta(result, (data) =>
            this.inventoryState.applyInventoryItemState(
              itemInstanceId,
              data.remainingInventoryItem,
            ),
          );
          this.showTradeReceipt(
            'Sold',
            response.filledQuantity,
            response.totalPrice - response.sellerFees,
            itemName,
            response.sellerFees,
          );
        }),
        map((result) => result.data),
      );
  }

  fulfillBuyOrder(
    buyOrderId: string,
    itemInstanceId: string,
    quantity: number,
  ): Observable<FulfillMarketPlaceBuyOrderResponse> {
    const fulfillment: FulfillMarketPlaceBuyOrderRequest = {
      marketPlaceBuyOrderId: buyOrderId,
      itemInstanceId,
      quantity,
    };

    return this.marketplaceService.fulfillBuyOrder(fulfillment).pipe(
      tap((result) => {
        this.applyFulfillBuyOrderResponse(result);
      }),
      map((result) => result.data),
    );
  }

  cancelListing(
    listingId: string,
  ): Observable<CancelMarketPlaceListingResponse> {
    return this.marketplaceService.cancelListing(listingId).pipe(
      tap((result) => {
        this.applyCancelResponse(result);
      }),
      map((result) => result.data),
    );
  }

  cancelBuyOrder(
    buyOrderId: string,
  ): Observable<CancelMarketPlaceBuyOrderResponse> {
    return this.marketplaceService.cancelBuyOrder(buyOrderId).pipe(
      tap((result) => {
        this.applyCancelBuyOrderResponse(result);
      }),
      map((result) => result.data),
    );
  }

  createListing(
    item: InventoryItem,
    quantity: number,
    unitPrice: number,
  ): Observable<CreateMarketPlaceListingResponse> {
    const listing: CreateMarketPlaceListingRequest = {
      itemInstanceId: item.itemInstance.id,
      quantity,
      unitPrice,
    };

    return this.marketplaceService.createListing(listing).pipe(
      tap((result) => {
        this.applyCreateResponse(result, item.itemInstance.itemBase.name);
      }),
      map((result) => result.data),
    );
  }

  createBuyOrder(
    itemBaseId: string,
    quantity: number,
    unitPrice: number,
  ): Observable<CreateMarketPlaceBuyOrderResponse> {
    const buyOrder: CreateMarketPlaceBuyOrderRequest = {
      itemBaseId,
      quantity,
      unitPrice,
    };

    return this.marketplaceService.createBuyOrder(buyOrder).pipe(
      tap((result) => {
        this.applyCreateBuyOrderResponse(result, itemBaseId);
      }),
      map((result) => result.data),
    );
  }

  removeListing(listingId: string): void {
    const filtered = this._listings().filter((l) => l.id !== listingId);
    this._listings.set(filtered);
  }

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
        return null;
      })
      .filter((l): l is MarketPlaceListing => l !== null);

    this._listings.set(updated);
  }

  private applyBuyoutResponse(
    result: VersionedMutationResult<BuyoutMarketPlaceListingResponse>,
  ): void {
    const response = result.data;
    this.applyVersionedMarketplace(result);
    this.inventoryState.applyVersionedInventoryDelta(result, (data) =>
      this.inventoryState.addOrIncrement(data.purchasedItem),
    );
    this.applyVersionedCinders(result, response.buyerCinders);
    this.showTradeReceipt(
      'Bought',
      response.purchasedQuantity,
      response.totalPrice,
      response.purchasedItem.itemInstance.itemBase.name,
    );
  }

  private applyCreateResponse(
    result: VersionedMutationResult<CreateMarketPlaceListingResponse>,
    itemName: string,
  ): void {
    const response = result.data;
    this.applyVersionedMarketplace(result);
    this.inventoryState.applyVersionedInventoryDelta(result, (data) =>
      this.inventoryState.applyInventoryItemState(
        data.listedItemInstanceId,
        data.remainingInventoryItem,
      ),
    );
    this.applyVersionedCinders(result, response.sellerCinders);
    if (response.filledQuantity > 0) {
      const remainder = response.listing
        ? ` ${this.formatNumber(response.listedQuantity)} listed at ${this.formatNumber(response.listing.unitPrice)} each.`
        : '';
      this.showTradeReceipt(
        'Sold',
        response.filledQuantity,
        response.filledTotalPrice - response.sellerFees,
        itemName,
        response.sellerFees,
        remainder,
      );
    } else if (response.listing) {
      this.toast.showToast(
        'Sell listing created',
        `${this.formatNumber(response.listedQuantity)} ${itemName} at ${this.formatNumber(response.listing.unitPrice)} Cinders each.`,
        true,
        't',
      );
    }
  }

  private applyCreateBuyOrderResponse(
    result: VersionedMutationResult<CreateMarketPlaceBuyOrderResponse>,
    itemBaseId: string,
  ): void {
    const response = result.data;
    this.applyVersionedMarketplace(result);

    this.applyVersionedCinders(result, response.buyerCinders);
    if (response.filledQuantity > 0) {
      const remainder = response.buyOrder
        ? ` ${this.formatNumber(response.buyOrder.quantity)} remain ordered at ${this.formatNumber(response.buyOrder.unitPrice)} each.`
        : '';
      this.showTradeReceipt(
        'Bought',
        response.filledQuantity,
        response.filledTotalPrice,
        this.itemName(itemBaseId),
        0,
        remainder,
      );
    } else if (response.buyOrder) {
      this.toast.showToast(
        'Buy order placed',
        `${this.formatNumber(response.buyOrder.quantity)} ${response.buyOrder.itemBase.name} at ${this.formatNumber(response.buyOrder.unitPrice)} Cinders each.`,
        true,
        't',
      );
    }
  }

  private applyFulfillBuyOrderResponse(
    result: VersionedMutationResult<FulfillMarketPlaceBuyOrderResponse>,
  ): void {
    const response = result.data;
    this.applyVersionedMarketplace(result);
    this.inventoryState.applyVersionedInventoryDelta(result, (data) =>
      this.inventoryState.applyInventoryItemState(
        data.soldItemInstanceId,
        data.remainingSellerInventoryItem,
      ),
    );
    this.applyVersionedCinders(result, response.sellerCinders);
    this.showTradeReceipt(
      'Sold',
      response.soldQuantity,
      response.totalPrice - response.sellerFee,
      response.purchasedItem.itemInstance.itemBase.name,
      response.sellerFee,
    );
  }

  private showTradeReceipt(
    action: 'Bought' | 'Sold',
    quantity: number,
    cinders: number,
    itemName?: string,
    fee = 0,
    suffix = '',
  ): void {
    const item = itemName ? ` ${itemName}` : '';
    const feeText =
      fee > 0 ? ` after ${this.formatNumber(fee)} Cinders in fees` : '';
    this.toast.showToast(
      'Trade completed',
      `${action} ${this.formatNumber(quantity)}${item} for ${this.formatNumber(cinders)} Cinders${feeText}.${suffix}`,
      true,
      't',
    );
  }

  private itemName(itemBaseId?: string): string | undefined {
    if (!itemBaseId) return undefined;
    return this._catalog().find((item) => item.id === itemBaseId)?.name;
  }

  private formatNumber(value: number): string {
    return value.toLocaleString();
  }

  private applyCancelResponse(
    result: VersionedMutationResult<CancelMarketPlaceListingResponse>,
  ): void {
    const response = result.data;
    this.applyVersionedMarketplace(result);
    this.inventoryState.applyVersionedInventoryDelta(result, (data) =>
      this.inventoryState.addOrIncrement(data.returnedItem),
    );
  }

  private applyCancelBuyOrderResponse(
    result: VersionedMutationResult<CancelMarketPlaceBuyOrderResponse>,
  ): void {
    const response = result.data;
    this.applyVersionedMarketplace(result);
    this.applyVersionedCinders(result, response.buyerCinders);
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

  private applyBuyOrderChange(
    buyOrderId: string,
    remainingBuyOrder: MarketPlaceBuyOrder | null,
  ): void {
    if (!remainingBuyOrder) {
      this.removeBuyOrder(buyOrderId);
      return;
    }

    this.upsertBuyOrder(remainingBuyOrder);
  }

  private updateCurrentCharacterCinders(cinders: number): void {
    const character = this.characterService.currentCharacter();
    if (!character) return;

    this.characterService.updateCharacter({
      ...character,
      cinders,
    });
  }

  private applyVersionedCinders<T>(
    result: VersionedMutationResult<T>,
    cinders: number,
  ): boolean {
    if (
      !this.domainVersions.isCurrent(
        'character',
        result.domainVersions['character'],
      )
    ) {
      return false;
    }

    this.updateCurrentCharacterCinders(cinders);
    return true;
  }

  private applyVersionedMarketplace<
    T extends { marketplace: MarketplaceChangeSet },
  >(result: VersionedMutationResult<T>): boolean {
    const changes = result.data.marketplace;
    const headerVersion = result.domainVersions['marketplace'];
    if (headerVersion !== undefined && headerVersion !== changes.version) {
      return false;
    }

    const currentVersion = this.stateSync.latestRevision('marketplace');
    if (changes.version < currentVersion) return false;
    if (changes.version === currentVersion) return true;
    if (changes.version > currentVersion + 1) {
      this.requestSemanticGapReconciliation(changes.version, true);
      return false;
    }

    this.applyMarketplaceChanges(changes);
    this.stateSync.acceptSnapshotResponse({ marketplace: changes.version }, [
      'marketplace',
    ]);
    return true;
  }

  private applySemanticChanges(changes: MarketplaceChangeSet): void {
    const currentVersion = this.domainVersions.latest('marketplace');
    if (changes.version <= currentVersion) return;

    if (changes.version > currentVersion + 1) {
      this.requestSemanticGapReconciliation(changes.version);
      return;
    }

    this.applyMarketplaceChanges(changes);
    this.domainVersions.observe({ marketplace: changes.version });
    this.stateSync.acceptSnapshotResponse({ marketplace: changes.version }, [
      'marketplace',
    ]);
  }

  private requestSemanticGapReconciliation(
    revision: number,
    rejectedMutationResponse = false,
  ): void {
    if (revision <= this.semanticGapRevision) return;
    this.semanticGapRevision = revision;
    if (rejectedMutationResponse) {
      this.stateSync.rejectMutationResponse('marketplace', revision);
      return;
    }
    this.stateSync.acceptInvalidation({
      characterId: null,
      scope: 'marketplace',
      revision,
      reason: 'Marketplace semantic sequence gap',
    });
  }

  private applyMarketplaceChanges(changes: MarketplaceChangeSet): void {
    // Any snapshot already in flight predates this ordered mutation/event.
    // Prevent it from replacing the newly applied marketplace state.
    this.refreshVersion += 1;
    this._loading.set(false);
    for (const change of changes.listingChanges) {
      this.applyListingChange(change.listingId, change.listing);
    }
    for (const change of changes.buyOrderChanges) {
      this.applyBuyOrderChange(change.buyOrderId, change.buyOrder);
    }

    const characterId = this.myCharacterId();
    if (!characterId || changes.orders.length === 0) return;

    const history = new Map(this._history().map((order) => [order.id, order]));
    for (const order of changes.orders) {
      if (order.buyerId === characterId || order.sellerId === characterId) {
        history.set(order.id, order);
      }
    }
    this._history.set(
      [...history.values()]
        .sort(
          (left, right) =>
            new Date(right.purchasedAt).getTime() -
            new Date(left.purchasedAt).getTime(),
        )
        .slice(0, 50),
    );
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

  private upsertBuyOrder(buyOrder: MarketPlaceBuyOrder): void {
    const buyOrders = this._buyOrders();
    const index = buyOrders.findIndex((current) => current.id === buyOrder.id);
    if (index === -1) {
      this._buyOrders.set([...buyOrders, buyOrder]);
      return;
    }

    const updated = [...buyOrders];
    updated[index] = buyOrder;
    this._buyOrders.set(updated);
  }

  private removeBuyOrder(buyOrderId: string): void {
    const filtered = this._buyOrders().filter(
      (order) => order.id !== buyOrderId,
    );
    this._buyOrders.set(filtered);
  }
}
