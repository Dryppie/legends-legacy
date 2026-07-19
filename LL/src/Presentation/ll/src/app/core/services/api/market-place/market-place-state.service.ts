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
import { GameEventService } from '../../real-time/game-event.service';
import { MarketListingSoldMsg } from '../../real-time/market/market-listing-sold';
import { MarketListingCreatedMsg } from '../../real-time/market/market-listing-created';
import { MarketListingCanceledMsg } from '../../real-time/market/market-listing-canceled';
import { MarketBuyOrderCreatedMsg } from '../../real-time/market/market-buy-order-created';
import { MarketBuyOrderFulfilledMsg } from '../../real-time/market/market-buy-order-fulfilled';
import { MarketBuyOrderCanceledMsg } from '../../real-time/market/market-buy-order-canceled';
import { GameEventDeduper } from '../../real-time/game-event/game-event-consumer';
import { ItemBase } from '../../../../shared/models/item';
import { MarketPlaceOrder } from '../../../../shared/models/Dtos/market-place/market-place-order';
import { ToastService } from '../../client-side/components/toast/toast.service';

@Injectable({ providedIn: 'root' })
export class MarketplaceStateService {
  private readonly _listings = signal<MarketPlaceListing[]>([]);
  private readonly _buyOrders = signal<MarketPlaceBuyOrder[]>([]);
  private readonly _catalog = signal<ItemBase[]>([]);
  private readonly _history = signal<MarketPlaceOrder[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  private readonly myCharacterId!: Signal<string | null>;
  private readonly eventDeduper = new GameEventDeduper();
  private hasLoaded = false;

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
    private eventService: GameEventService,
    private toast: ToastService,
  ) {
    this.myCharacterId = computed(() => this.characterService.currentCharacterId());

    effect(
      () => {
        const saleEnvelope =
          this.eventService.eventEnvelope.MarketListingSoldMsg();
        const createdEnvelope =
          this.eventService.eventEnvelope.MarketListingCreatedMsg();
        const canceledEnvelope =
          this.eventService.eventEnvelope.MarketListingCanceledMsg();
        const buyOrderCreatedEnvelope =
          this.eventService.eventEnvelope.MarketBuyOrderCreatedMsg();
        const buyOrderFulfilledEnvelope =
          this.eventService.eventEnvelope.MarketBuyOrderFulfilledMsg();
        const buyOrderCanceledEnvelope =
          this.eventService.eventEnvelope.MarketBuyOrderCanceledMsg();

        const sale = saleEnvelope?.payload;
        const created = createdEnvelope?.payload;
        const canceled = canceledEnvelope?.payload;
        const buyOrderCreated = buyOrderCreatedEnvelope?.payload;
        const buyOrderFulfilled = buyOrderFulfilledEnvelope?.payload;
        const buyOrderCanceled = buyOrderCanceledEnvelope?.payload;

        if (sale && this.eventDeduper.shouldProcess('sold', saleEnvelope)) {
          untracked(() => this.applySellerSale(sale));
        }

        if (
          created &&
          this.eventDeduper.shouldProcess('created', createdEnvelope)
        ) {
          untracked(() => this.applyCreatedListing(created));
        }

        if (
          canceled &&
          this.eventDeduper.shouldProcess('canceled', canceledEnvelope)
        ) {
          untracked(() => this.applyCanceledListing(canceled));
        }

        if (
          buyOrderCreated &&
          this.eventDeduper.shouldProcess('buy-order-created', buyOrderCreatedEnvelope)
        ) {
          untracked(() => this.applyCreatedBuyOrder(buyOrderCreated));
        }

        if (
          buyOrderFulfilled &&
          this.eventDeduper.shouldProcess('buy-order-fulfilled', buyOrderFulfilledEnvelope)
        ) {
          untracked(() => this.applyFulfilledBuyOrder(buyOrderFulfilled));
        }

        if (
          buyOrderCanceled &&
          this.eventDeduper.shouldProcess('buy-order-canceled', buyOrderCanceledEnvelope)
        ) {
          untracked(() => this.applyCanceledBuyOrder(buyOrderCanceled));
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
    if (this._listings().length || this._buyOrders().length) return;
    this.refresh();
  }

  refresh(): void {
    this.hasLoaded = true;
    this._loading.set(true);

    this.marketplaceService
      .getListings()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (marketplaceListings) => {
          const sorted = marketplaceListings
            .slice()
            .sort((a, b) =>
              a.itemInstance.itemBase.itemType.localeCompare(
                b.itemInstance.itemBase.itemType,
              ),
            );

          this._listings.set(sorted);
        },
        error: (err) => this._error.set(err.message ?? 'Unknown error'),
      });

    this.marketplaceService.getCatalog().subscribe({
      next: (items) => this._catalog.set(items),
      error: (err) => this._error.set(err.message ?? 'Unknown error'),
    });

    this.marketplaceService.getHistory().subscribe({
      next: (orders) => this._history.set(orders),
      error: (err) => this._error.set(err.message ?? 'Unknown error'),
    });

    this.marketplaceService.getBuyOrders().subscribe({
      next: (buyOrders) => {
        const sorted = buyOrders
          .slice()
          .sort((a, b) => a.itemBase.itemType.localeCompare(b.itemBase.itemType));

        this._buyOrders.set(sorted);
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

  buyCommodity(
    itemBaseId: string,
    quantity: number,
    maximumUnitPrice: number,
  ): Observable<BuyCommodityResponse> {
    return this.marketplaceService
      .buyCommodity(itemBaseId, quantity, maximumUnitPrice)
      .pipe(
        tap((response) => {
          this.updateCurrentCharacterCinders(response.buyerCinders);
          this.inventoryState.load(true);
          this.refresh();
          this.showTradeReceipt(
            'Bought',
            response.filledQuantity,
            response.totalPrice,
            this.itemName(itemBaseId),
          );
        }),
      );
  }

  sellCommodity(
    itemInstanceId: string,
    quantity: number,
    minimumUnitPrice: number,
  ): Observable<SellCommodityResponse> {
    const itemName = this.inventoryState
      .items()
      .find((item) => item.itemInstance.id === itemInstanceId)
      ?.itemInstance.itemBase.name;
    return this.marketplaceService
      .sellCommodity(itemInstanceId, quantity, minimumUnitPrice)
      .pipe(
        tap((response) => {
          this.updateCurrentCharacterCinders(response.sellerCinders);
          this.inventoryState.applyInventoryItemState(
            itemInstanceId,
            response.remainingInventoryItem,
          );
          this.refresh();
          this.showTradeReceipt(
            'Sold',
            response.filledQuantity,
            response.totalPrice - response.sellerFees,
            itemName,
            response.sellerFees,
          );
        }),
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
      tap((response) => {
        this.applyFulfillBuyOrderResponse(response);
      }),
    );
  }

  cancelListing(listingId: string): Observable<CancelMarketPlaceListingResponse> {
    return this.marketplaceService.cancelListing(listingId).pipe(
      tap((response) => {
        this.applyCancelResponse(response);
      }),
    );
  }

  cancelBuyOrder(
    buyOrderId: string,
  ): Observable<CancelMarketPlaceBuyOrderResponse> {
    return this.marketplaceService.cancelBuyOrder(buyOrderId).pipe(
      tap((response) => {
        this.applyCancelBuyOrderResponse(response);
      }),
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
      tap((response) => {
        this.applyCreateResponse(
          response,
          item.itemInstance.itemBase.name,
        );
      }),
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
      tap((response) => {
        this.applyCreateBuyOrderResponse(response, itemBaseId);
      }),
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

  private applyBuyoutResponse(response: BuyoutMarketPlaceListingResponse): void {
    this.applyListingChange(response.listingId, response.remainingListing);
    this.inventoryState.addOrIncrement(response.purchasedItem);
    this.updateCurrentCharacterCinders(response.buyerCinders);
    this.showTradeReceipt(
      'Bought',
      response.purchasedQuantity,
      response.totalPrice,
      response.purchasedItem.itemInstance.itemBase.name,
    );
  }

  private applyCreateResponse(
    response: CreateMarketPlaceListingResponse,
    itemName: string,
  ): void {
    if (response.listing) {
      this.upsertListing(response.listing);
    }
    this.inventoryState.applyInventoryItemState(
      response.listedItemInstanceId,
      response.remainingInventoryItem,
    );
    this.updateCurrentCharacterCinders(response.sellerCinders);
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
    response: CreateMarketPlaceBuyOrderResponse,
    itemBaseId: string,
  ): void {
    if (response.buyOrder) {
      this.upsertBuyOrder(response.buyOrder);
    }

    this.updateCurrentCharacterCinders(response.buyerCinders);
    if (response.filledQuantity > 0) {
      this.inventoryState.load(true);
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
    this.refresh();
  }

  private applyFulfillBuyOrderResponse(
    response: FulfillMarketPlaceBuyOrderResponse,
  ): void {
    this.applyBuyOrderChange(response.buyOrderId, response.remainingBuyOrder);
    this.inventoryState.applyInventoryItemState(
      response.soldItemInstanceId,
      response.remainingSellerInventoryItem,
    );
    this.updateCurrentCharacterCinders(response.sellerCinders);
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

  private applyCancelResponse(response: CancelMarketPlaceListingResponse): void {
    this.removeListing(response.listingId);
    this.inventoryState.addOrIncrement(response.returnedItem);
  }

  private applyCancelBuyOrderResponse(
    response: CancelMarketPlaceBuyOrderResponse,
  ): void {
    this.removeBuyOrder(response.buyOrderId);
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

  private applyCreatedBuyOrder(event: MarketBuyOrderCreatedMsg): void {
    this.upsertBuyOrder(event.buyOrder);
  }

  private applyFulfilledBuyOrder(event: MarketBuyOrderFulfilledMsg): void {
    this.applyBuyOrderChange(event.buyOrderId, event.remainingBuyOrder);

    if (event.buyerId === this.myCharacterId()) {
      this.inventoryState.addOrIncrement(event.purchasedItem);
    }

    if (event.sellerId === this.myCharacterId()) {
      this.updateCurrentCharacterCinders(event.sellerCinders);
    }
  }

  private applyCanceledBuyOrder(event: MarketBuyOrderCanceledMsg): void {
    this.removeBuyOrder(event.buyOrderId);
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
    const filtered = this._buyOrders().filter((order) => order.id !== buyOrderId);
    this._buyOrders.set(filtered);
  }
}
