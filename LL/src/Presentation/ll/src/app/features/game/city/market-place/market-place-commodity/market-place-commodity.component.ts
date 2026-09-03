import { QuestStateService } from '../../../../../core/services/api/quest/quest-state.service';
import { itemDescription } from '../../../../../shared/utils/inventory/item-description';
import { CommonModule } from '@angular/common';
import {
  Component,
  computed,
  effect,
  EventEmitter,
  Input,
  OnInit,
  Output,
  signal,
  untracked,
} from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  debounceTime,
  distinctUntilChanged,
  map,
  startWith,
} from 'rxjs/operators';

import { CharacterService } from '../../../../../core/services/api/character/character.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { MarketplaceStateService } from '../../../../../core/services/api/market-place/market-place-state.service';
import {
  MarketPlaceItemSummary,
  MarketPlaceService,
} from '../../../../../core/services/api/market-place/market-place.service';
import { MarketPlaceListing } from '../../../../../shared/models/Dtos/market-place/market-place-listing';
import { MarketPlaceBuyOrder } from '../../../../../shared/models/Dtos/market-place/market-place-buy-order';
import {
  EssenceItem,
  ItemBase,
  inferEssenceDefinitionId,
} from '../../../../../shared/models/item';
import { EssenceStateService } from '../../../../../core/services/api/essences/essence-state.service';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';
import { MarketCategoryId } from '../../../../../shared/models/market-category';
import {
  getMarketplaceResourceSortRank,
  isMarketplaceBlueprintResource,
  matchesMarketplaceResourceSubcategory,
} from '../../../../../shared/utils/market-place/market-place-category.utils';
import { EssenceDescriptionComponent } from '../../../../../shared/components/essences/essence-description/essence-description.component';
import { AbilityTagsComponent } from '../../../../../shared/components/essences/ability-tags/ability-tags.component';
import { marketplaceCommoditySearchText } from './market-place-commodity-search';

interface Commodity {
  base: ItemBase;
  ownedQuantity: number;
  listedQuantity: number;
  buyOrderQuantity: number;
  bestSellPrice: number | null;
  bestBuyPrice: number | null;
}

interface CommodityOrderRow {
  unitPrice: number;
  quantity: number;
  ownQuantity: number;
  listings: MarketPlaceListing[];
}

interface CommodityBuyOrderRow {
  unitPrice: number;
  quantity: number;
  ownQuantity: number;
  orders: MarketPlaceBuyOrder[];
}

type CommodityCatalogStatus = 'all' | 'active' | 'owned' | 'unabsorbed';
type CommodityCatalogSort = 'activity' | 'name' | 'ask';
type MobileOrderBook = 'sell' | 'buy';
type MarketTicketSide = 'buy' | 'sell';

@Component({
  selector: 'app-market-place-commodity',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NumberFormatPipe,
    EssenceDescriptionComponent,
    AbilityTagsComponent,
  ],
  templateUrl: './market-place-commodity.component.html',
  styleUrl: './market-place-commodity.component.css',
})
export class MarketPlaceCommodityComponent implements OnInit {
  private readonly _itemType = signal<ItemType>(ItemType.Resource);
  private readonly _subcategory = signal<string | null>(null);
  private readonly _category = signal<MarketCategoryId>('resources');

  @Input({ required: true })
  set itemType(value: ItemType) {
    this._itemType.set(value);
    this.resetMobileView();
  }

  @Input()
  set subcategory(value: string | null) {
    this._subcategory.set(value);
    this.resetMobileView();
  }

  @Input({ required: true })
  set category(value: MarketCategoryId) {
    this._category.set(value);
    // "Not absorbed" only exists for Essences; leaving the category would strand the select on
    // a value it no longer offers.
    if (value !== 'essences' && this.catalogStatus() === 'unabsorbed') {
      this.catalogStatus.set('all');
    }
    this.resetMobileView();
  }

  @Output() readonly mobileDetailChanged = new EventEmitter<boolean>();

  readonly selectedSellPrice = signal<number | null>(null);
  readonly selectedBuyPrice = signal<number | null>(null);
  readonly selectedCommodityId = signal<string | null>(null);
  readonly placingOrder = signal(false);
  readonly marketSummary = signal<MarketPlaceItemSummary | null>(null);
  readonly catalogStatus = signal<CommodityCatalogStatus>('all');
  readonly catalogSort = signal<CommodityCatalogSort>('activity');
  readonly mobileDetailOpen = signal(false);
  readonly mobileOrderBook = signal<MobileOrderBook>('sell');
  readonly ticketSide = signal<MarketTicketSide>('buy');

  readonly quantityCtrl = new FormControl<number>(1, {
    validators: [Validators.required, Validators.min(1)],
  });
  readonly unitPriceCtrl = new FormControl<number | null>(null, {
    validators: [Validators.required, Validators.min(1)],
  });
  readonly catalogSearchCtrl = new FormControl<string>('', {
    nonNullable: true,
    validators: [Validators.maxLength(64)],
  });
  readonly catalogSearch = toSignal(
    this.catalogSearchCtrl.valueChanges.pipe(
      startWith(this.catalogSearchCtrl.value),
      debounceTime(150),
      map((value) => value.trim().toLowerCase()),
      distinctUntilChanged(),
    ),
    { initialValue: '' },
  );

  readonly currentCharacterId = computed(() =>
    this.characterService.currentCharacterId(),
  );
  readonly currentCinders = computed(
    () => this.characterService.currentCharacter()?.cinders ?? 0,
  );

  readonly listings = computed(() =>
    this.marketplaceState
      .listings()
      .filter((listing) => this.matchesCommodityType(listing)),
  );

  readonly buyOrders = computed(() =>
    this.marketplaceState
      .buyOrders()
      .filter((order) => this.matchesCommodityBase(order.itemBase)),
  );

  readonly inventory = computed(() =>
    this.inventoryState
      .items()
      .filter((item) => this.matchesCommodityType(item)),
  );

  readonly commodities = computed(() => {
    const byBase = new Map<string, Commodity>();

    for (const base of this.marketplaceState.catalog()) {
      if (!this.matchesCommodityBase(base)) continue;

      byBase.set(base.id, {
        base,
        ownedQuantity: 0,
        listedQuantity: 0,
        buyOrderQuantity: 0,
        bestSellPrice: null,
        bestBuyPrice: null,
      });
    }

    for (const item of this.inventory()) {
      const base = item.itemInstance.itemBase;
      const existing = byBase.get(base.id);
      byBase.set(base.id, {
        base,
        ownedQuantity: (existing?.ownedQuantity ?? 0) + item.quantity,
        listedQuantity: existing?.listedQuantity ?? 0,
        buyOrderQuantity: existing?.buyOrderQuantity ?? 0,
        bestSellPrice: existing?.bestSellPrice ?? null,
        bestBuyPrice: existing?.bestBuyPrice ?? null,
      });
    }

    for (const listing of this.listings()) {
      const base = listing.itemInstance.itemBase;
      const existing = byBase.get(base.id);
      const best = existing?.bestSellPrice ?? null;
      byBase.set(base.id, {
        base,
        ownedQuantity: existing?.ownedQuantity ?? 0,
        listedQuantity: (existing?.listedQuantity ?? 0) + listing.quantity,
        buyOrderQuantity: existing?.buyOrderQuantity ?? 0,
        bestSellPrice:
          best === null ? listing.unitPrice : Math.min(best, listing.unitPrice),
        bestBuyPrice: existing?.bestBuyPrice ?? null,
      });
    }

    for (const order of this.buyOrders()) {
      const base = order.itemBase;
      const existing = byBase.get(base.id);
      const best = existing?.bestBuyPrice ?? null;
      byBase.set(base.id, {
        base,
        ownedQuantity: existing?.ownedQuantity ?? 0,
        listedQuantity: existing?.listedQuantity ?? 0,
        buyOrderQuantity: (existing?.buyOrderQuantity ?? 0) + order.quantity,
        bestSellPrice: existing?.bestSellPrice ?? null,
        bestBuyPrice:
          best === null ? order.unitPrice : Math.max(best, order.unitPrice),
      });
    }

    return [...byBase.values()].sort((a, b) => {
      const blueprintRank =
        Number(isMarketplaceBlueprintResource(b.base)) -
        Number(isMarketplaceBlueprintResource(a.base));
      if (blueprintRank !== 0) return blueprintRank;

      const tierRank =
        getMarketplaceResourceSortRank(a.base, this._subcategory()) -
        getMarketplaceResourceSortRank(b.base, this._subcategory());
      if (tierRank !== 0) return tierRank;

      return a.base.name.localeCompare(b.base.name);
    });
  });

  readonly selectedCommodity = computed(() => {
    const commodities = this.commodities();
    if (!commodities.length) return null;

    return (
      commodities.find(
        (commodity) => commodity.base.id === this.selectedCommodityId(),
      ) ?? commodities[0]
    );
  });

  readonly isEssenceCatalogue = computed(() => this._category() === 'essences');

  /** Essence definition ids already held in the Soul Archive. */
  readonly absorbedEssenceDefinitionIds = computed(() =>
    this.essenceState.absorbedEssenceDefinitionIds(),
  );

  isEssenceCommodity(base: ItemBase): boolean {
    return base.itemType === ItemType.Essence;
  }

  /** True when this catalogue entry is an Essence already absorbed into the Soul Archive. */
  isAbsorbedEssence(base: ItemBase): boolean {
    if (!this.isEssenceCommodity(base)) return false;

    return this.absorbedEssenceDefinitionIds().has(
      inferEssenceDefinitionId(base as EssenceItem),
    );
  }

  readonly selectedEssenceDefinition = computed(() => {
    const base = this.selectedCommodity()?.base;
    if (base?.itemType !== ItemType.Essence) return null;

    return (base as EssenceItem).essence ?? null;
  });

  readonly filteredCommodities = computed(() => {
    const query = this.catalogSearch();
    const status = this.catalogStatus();
    const sort = this.catalogSort();
    let commodities = this.commodities().filter((commodity) => {
      if (
        query &&
        !marketplaceCommoditySearchText(commodity.base).includes(query)
      ) {
        return false;
      }

      if (status === 'active') {
        return commodity.listedQuantity > 0 || commodity.buyOrderQuantity > 0;
      }

      if (status === 'owned') {
        return commodity.ownedQuantity > 0;
      }

      if (status === 'unabsorbed') {
        return !this.isAbsorbedEssence(commodity.base);
      }

      return true;
    });

    commodities = commodities.slice().sort((a, b) => {
      if (sort === 'name') {
        return a.base.name.localeCompare(b.base.name);
      }

      if (sort === 'ask') {
        const aPrice = a.bestSellPrice ?? Number.MAX_SAFE_INTEGER;
        const bPrice = b.bestSellPrice ?? Number.MAX_SAFE_INTEGER;
        return aPrice - bPrice || a.base.name.localeCompare(b.base.name);
      }

      const aActivity = a.listedQuantity + a.buyOrderQuantity;
      const bActivity = b.listedQuantity + b.buyOrderQuantity;
      return bActivity - aActivity || a.base.name.localeCompare(b.base.name);
    });

    return commodities;
  });

  readonly catalogueTitle = computed(() => {
    switch (this._category()) {
      case 'blueprints':
        return 'Blueprint catalogue';
      case 'catalysts':
        return 'Catalyst catalogue';
      case 'essences':
        return 'Essence catalogue';
      default:
        return `${this._subcategory() ?? 'Resource'} market`;
    }
  });

  readonly catalogueHeading = computed(() => {
    switch (this._category()) {
      case 'blueprints':
        return 'Blueprints';
      case 'catalysts':
        return 'Catalysts';
      case 'essences':
        return 'Essences';
      default:
        return this._subcategory() ?? 'Resources';
    }
  });

  readonly catalogueSearchPlaceholder = computed(() =>
    this._category() === 'essences'
      ? 'Search essence, ability, or tag...'
      : 'Search catalogue...',
  );

  readonly selectedListings = computed(() => {
    const selected = this.selectedCommodity();
    if (!selected) return [];

    return this.listings().filter(
      (listing) => listing.itemInstance.itemBase.id === selected.base.id,
    );
  });

  readonly selectedBuyOrders = computed(() => {
    const selected = this.selectedCommodity();
    if (!selected) return [];

    return this.buyOrders().filter(
      (order) => order.itemBase.id === selected.base.id,
    );
  });

  readonly marketName = computed(() => {
    return (
      this.selectedCommodity()?.base.name ||
      this._subcategory() ||
      this._itemType()
    );
  });

  readonly selectedInventoryItem = computed(() => {
    const selected = this.selectedCommodity();
    if (!selected) return null;

    return (
      this.inventory().find(
        (item) => item.itemInstance.itemBase.id === selected.base.id,
      ) ?? null
    );
  });

  readonly sellOrderRows = computed(() => {
    const grouped = new Map<number, CommodityOrderRow>();
    const characterId = this.currentCharacterId();
    for (const listing of this.selectedListings()) {
      const existing = grouped.get(listing.unitPrice);
      if (existing) {
        existing.quantity += listing.quantity;
        existing.ownQuantity +=
          listing.sellerId === characterId ? listing.quantity : 0;
        existing.listings.push(listing);
      } else {
        grouped.set(listing.unitPrice, {
          unitPrice: listing.unitPrice,
          quantity: listing.quantity,
          ownQuantity: listing.sellerId === characterId ? listing.quantity : 0,
          listings: [listing],
        });
      }
    }

    return [...grouped.values()].sort((a, b) => a.unitPrice - b.unitPrice);
  });

  readonly buyOrderRows = computed(() => {
    const grouped = new Map<number, CommodityBuyOrderRow>();
    const characterId = this.currentCharacterId();
    for (const order of this.selectedBuyOrders()) {
      const existing = grouped.get(order.unitPrice);
      if (existing) {
        existing.quantity += order.quantity;
        existing.ownQuantity +=
          order.buyerId === characterId ? order.quantity : 0;
        existing.orders.push(order);
      } else {
        grouped.set(order.unitPrice, {
          unitPrice: order.unitPrice,
          quantity: order.quantity,
          ownQuantity: order.buyerId === characterId ? order.quantity : 0,
          orders: [order],
        });
      }
    }

    return [...grouped.values()].sort((a, b) => b.unitPrice - a.unitPrice);
  });

  readonly ownedQuantity = computed(
    () => this.selectedCommodity()?.ownedQuantity ?? 0,
  );

  readonly ownSellListingForSelectedItem = computed(() => {
    const itemBaseId = this.selectedCommodity()?.base.id;
    if (!itemBaseId) return null;

    return (
      this.marketplaceState
        .myListings()
        .find((listing) => listing.itemInstance.itemBase.id === itemBaseId) ??
      null
    );
  });

  readonly ownBuyOrderForSelectedItem = computed(() => {
    const itemBaseId = this.selectedCommodity()?.base.id;
    if (!itemBaseId) return null;

    return (
      this.marketplaceState
        .myBuyOrders()
        .find((order) => order.itemBaseId === itemBaseId) ?? null
    );
  });

  readonly hasOwnSellListingsForSelectedItem = computed(
    () => this.ownSellListingForSelectedItem() !== null,
  );

  readonly hasOwnBuyOrdersForSelectedItem = computed(
    () => this.ownBuyOrderForSelectedItem() !== null,
  );

  readonly hasOwnOrderForSelectedItem = computed(
    () =>
      this.hasOwnSellListingsForSelectedItem() ||
      this.hasOwnBuyOrdersForSelectedItem(),
  );

  readonly buyOrderActionLabel = computed(() => {
    if (this.hasOwnSellListingsForSelectedItem()) return 'Cancel sell order';
    if (this.hasOwnBuyOrdersForSelectedItem()) return 'Cancel buy order';
    return 'Place buy order';
  });

  readonly sellOrderActionLabel = computed(() => {
    if (this.hasOwnSellListingsForSelectedItem()) return 'Cancel sell order';
    if (this.hasOwnBuyOrdersForSelectedItem()) return 'Cancel buy order';
    return 'List for sale';
  });

  readonly bestSellPrice = computed(() => {
    return this.selectedCommodity()?.bestSellPrice ?? null;
  });

  readonly bestBuyPrice = computed(() => {
    return this.selectedCommodity()?.bestBuyPrice ?? null;
  });

  readonly selectedSellOrderIsOwn = computed(() => {
    const selectedPrice = this.selectedSellPrice();
    const characterId = this.currentCharacterId();
    if (selectedPrice === null || !characterId) return false;

    const selectedRow = this.sellOrderRows().find(
      (row) => row.unitPrice === selectedPrice,
    );
    return (
      !!selectedRow?.ownQuantity &&
      this.purchasableSellQuantityAtOrBelow(selectedPrice) === 0
    );
  });

  readonly spread = computed(() => {
    const summary = this.marketSummary();
    if (
      !summary ||
      summary.lowestSellUnitPrice === null ||
      summary.highestBuyUnitPrice === null
    ) {
      return null;
    }

    return summary.lowestSellUnitPrice - summary.highestBuyUnitPrice;
  });
  readonly spreadValue = computed(() => this.spread() ?? undefined);
  readonly lastTradePrice = computed(
    () => this.marketSummary()?.lastTradeUnitPrice ?? undefined,
  );
  readonly medianPrice7Days = computed(
    () => this.marketSummary()?.medianUnitPrice7Days ?? undefined,
  );

  readonly itemDescription = itemDescription;


  constructor(
    private readonly inventoryState: InventoryStateService,
    private readonly marketplaceState: MarketplaceStateService,
    private readonly characterService: CharacterService,
    private readonly marketplaceService: MarketPlaceService,
    private readonly essenceState: EssenceStateService,
    private readonly questState: QuestStateService,
  ) {
    // The Soul Archive snapshot is normally only fetched by the Essences page, so pull it in the
    // first time the Essence catalogue is shown. Both dependencies are stable while the request
    // is in flight, so this runs once rather than per change detection.
    effect(() => {
      if (!this.isEssenceCatalogue()) return;
      if (this.essenceState.archive()) return;

      untracked(() => this.essenceState.refreshArchive());
    });

    effect(() => {
      this._itemType();
      this._subcategory();
      this.selectedCommodityId();
      const ticketSide = untracked(() => this.ticketSide());
      this.quantityCtrl.setValue(1, { emitEvent: false });
      this.unitPriceCtrl.setValue(
        ticketSide === 'buy'
          ? (this.bestSellPrice() ?? this.bestBuyPrice())
          : (this.bestBuyPrice() ?? this.bestSellPrice()),
        {
          emitEvent: false,
        },
      );
    });

    effect(() => {
      const itemBaseId = this.selectedCommodity()?.base.id;
      this.marketplaceState.listings();
      this.marketplaceState.buyOrders();
      if (!itemBaseId) {
        this.marketSummary.set(null);
        return;
      }

      this.marketplaceService.getSummary(itemBaseId).subscribe({
        next: (summary) => {
          if (this.selectedCommodity()?.base.id === summary.itemBaseId) {
            this.marketSummary.set(summary);
          }
        },
        error: () => this.marketSummary.set(null),
      });
    });

    effect(() => {
      const commodities = this.commodities();
      const selectedCommodityId = this.selectedCommodityId();
      if (!commodities.length) {
        this.selectedCommodityId.set(null);
        return;
      }

      if (
        !selectedCommodityId ||
        !commodities.some(
          (commodity) => commodity.base.id === selectedCommodityId,
        )
      ) {
        this.selectedCommodityId.set(commodities[0].base.id);
      }
    });

    effect(() => {
      this.selectedCommodityId();
      const firstSellOrder = this.sellOrderRows()[0];
      const current = this.selectedSellPrice();
      if (!firstSellOrder) {
        this.selectedSellPrice.set(null);
        return;
      }

      if (
        !current ||
        !this.sellOrderRows().some((row) => row.unitPrice === current)
      ) {
        this.selectSellOrder(firstSellOrder, false);
      }
    });

    effect(() => {
      this.selectedCommodityId();
      const firstBuyOrder = this.buyOrderRows()[0];
      const current = this.selectedBuyPrice();
      if (!firstBuyOrder) {
        this.selectedBuyPrice.set(null);
        return;
      }

      if (
        !current ||
        !this.buyOrderRows().some((row) => row.unitPrice === current)
      ) {
        this.selectBuyOrder(firstBuyOrder, false);
      }
    });
  }

  ngOnInit(): void {
    this.inventoryState.load();
    this.marketplaceState.load();
  }

  selectSellOrder(row: CommodityOrderRow, updateQuantity = true): void {
    this.selectedSellPrice.set(row.unitPrice);
    this.unitPriceCtrl.setValue(row.unitPrice, { emitEvent: false });
    if (updateQuantity) {
      const cumulativeQuantity = this.purchasableSellQuantityAtOrBelow(
        row.unitPrice,
      );
      this.quantityCtrl.setValue(
        cumulativeQuantity > 0 ? cumulativeQuantity : row.quantity,
      );
    }
  }

  selectBuyOrder(row: CommodityBuyOrderRow, updateQuantity = true): void {
    this.selectedBuyPrice.set(row.unitPrice);
    this.unitPriceCtrl.setValue(row.unitPrice, { emitEvent: false });
    if (updateQuantity) {
      this.quantityCtrl.setValue(row.quantity);
    }
  }

  selectCommodity(commodity: Commodity): void {
    this.selectedCommodityId.set(commodity.base.id);
    this.mobileDetailOpen.set(true);
    this.mobileOrderBook.set('sell');
    this.mobileDetailChanged.emit(true);
  }

  closeMobileDetail(): void {
    this.mobileDetailOpen.set(false);
    this.mobileDetailChanged.emit(false);
  }

  setMobileOrderBook(orderBook: MobileOrderBook): void {
    this.mobileOrderBook.set(orderBook);
    const selectedPrice =
      orderBook === 'sell'
        ? this.selectedSellPrice()
        : (this.selectedBuyPrice() ??
          this.bestBuyPrice() ??
          this.bestSellPrice());
    this.unitPriceCtrl.setValue(selectedPrice, { emitEvent: false });
  }

  adjustMobileQuantity(delta: number): void {
    // Treat a cleared input as 0 so the first tap on "+" lands on 1.
    const current = this.quantityCtrl.value ?? 0;
    const quantity = Math.max(1, current + delta);
    this.quantityCtrl.setValue(quantity);
  }

  submitMobileOrder(): void {
    if (this.ticketSide() === 'sell') {
      this.submitSellOrderAction();
      return;
    }

    if (this.mobileOrderBook() === 'buy') {
      this.submitBuyOrderAction();
      return;
    }

    this.buySelectedCommodity();
  }

  setTicketSide(side: MarketTicketSide): void {
    this.ticketSide.set(side);
  }

  submitBuyOrderAction(): void {
    if (this.hasOwnOrderForSelectedItem()) {
      this.cancelActiveOrderForSelectedItem();
      return;
    }

    this.placeBuyOrder();
  }

  submitSellOrderAction(): void {
    if (this.hasOwnOrderForSelectedItem()) {
      this.cancelActiveOrderForSelectedItem();
      return;
    }

    this.sellSelectedCommodity();
  }

  setCatalogStatus(value: string): void {
    if (
      value === 'all' ||
      value === 'active' ||
      value === 'owned' ||
      value === 'unabsorbed'
    ) {
      this.catalogStatus.set(value);
    }
  }

  setCatalogSort(value: string): void {
    if (value === 'activity' || value === 'name' || value === 'ask') {
      this.catalogSort.set(value);
    }
  }

  commodityDisplayName(commodity: Commodity): string {
    return this._category() === 'blueprints'
      ? commodity.base.name.replace(/^Blueprint:\s*/i, '')
      : commodity.base.name;
  }

  isFavoriteCommodity(commodity: Commodity): boolean {
    return this.inventory().some(
      (item) =>
        item.itemInstance.itemBase.id === commodity.base.id &&
        !!item.isFavorite,
    );
  }

  commodityDisplayPrice(commodity: Commodity): number | undefined {
    return commodity.bestSellPrice ?? commodity.bestBuyPrice ?? undefined;
  }

  buyOrderCommitment(): number {
    const quantity = this.quantityCtrl.value ?? 0;
    const limitPrice = this.unitPriceCtrl.value ?? 0;
    if (quantity <= 0 || limitPrice <= 0) return 0;

    const characterId = this.currentCharacterId();
    let remaining = quantity;
    let total = 0;
    const eligibleListings = this.selectedListings()
      .filter(
        (listing) =>
          listing.sellerId !== characterId && listing.unitPrice <= limitPrice,
      )
      .slice()
      .sort(
        (a, b) =>
          a.unitPrice - b.unitPrice ||
          a.createdAt.toString().localeCompare(b.createdAt.toString()),
      );

    for (const listing of eligibleListings) {
      const fillQuantity = Math.min(remaining, listing.quantity);
      total += fillQuantity * listing.unitPrice;
      remaining -= fillQuantity;
      if (remaining === 0) break;
    }

    return total + remaining * limitPrice;
  }

  ticketOrderValue(): number {
    if (this.ticketSide() === 'buy') return this.buyOrderCommitment();

    return (this.quantityCtrl.value ?? 0) * (this.unitPriceCtrl.value ?? 0);
  }

  sellSelectedCommodity(): void {
    const item = this.selectedInventoryItem();
    if (!item || !this.canSell()) return;

    const quantity = this.quantityCtrl.value!;
    const unitPrice = this.unitPriceCtrl.value!;
    if (quantity > this.ownedQuantity()) return;

    this.placingOrder.set(true);
    this.marketplaceState.createListing(item, quantity, unitPrice).subscribe({
      error: (error) => {
        console.error(error);
        this.placingOrder.set(false);
      },
      complete: () => this.placingOrder.set(false),
    });
  }

  placeBuyOrder(): void {
    const selected = this.selectedCommodity();
    if (!selected || !this.canPlaceBuyOrder()) return;

    const quantity = this.quantityCtrl.value!;
    const unitPrice = this.unitPriceCtrl.value!;

    this.placingOrder.set(true);
    this.marketplaceState
      .createBuyOrder(selected.base.id, quantity, unitPrice)
      .subscribe({
        error: (error) => {
          console.error(error);
          this.placingOrder.set(false);
        },
        complete: () => this.placingOrder.set(false),
      });
  }

  buySelectedCommodity(): void {
    const commodity = this.selectedCommodity();
    if (!commodity || this.quantityCtrl.invalid || this.unitPriceCtrl.invalid)
      return;

    this.placingOrder.set(true);
    this.marketplaceState
      .buyCommodity(
        commodity.base.id,
        this.quantityCtrl.value!,
        this.unitPriceCtrl.value!,
      )
      .subscribe({
        error: (error) => {
          console.error(error);
          this.placingOrder.set(false);
        },
        complete: () => this.placingOrder.set(false),
      });
  }

  fillSelectedBuyOrder(): void {
    const item = this.selectedInventoryItem();
    if (!item || !this.canFillBuyOrder()) return;

    this.placingOrder.set(true);
    this.marketplaceState
      .sellCommodity(
        item.itemInstance.id,
        this.quantityCtrl.value!,
        this.unitPriceCtrl.value!,
      )
      .subscribe({
        error: (error) => {
          console.error(error);
          this.placingOrder.set(false);
        },
        complete: () => this.placingOrder.set(false),
      });
  }

  cancelActiveOrderForSelectedItem(): void {
    if (this.placingOrder()) return;

    this.placingOrder.set(true);
    const sellListing = this.ownSellListingForSelectedItem();
    const buyOrder = this.ownBuyOrderForSelectedItem();
    const cancellation: Observable<unknown> | null = sellListing
      ? this.marketplaceState.cancelListing(sellListing.id)
      : buyOrder
        ? this.marketplaceState.cancelBuyOrder(buyOrder.id)
        : null;

    if (!cancellation) {
      this.placingOrder.set(false);
      return;
    }

    cancellation.subscribe({
      error: (error) => {
        console.error(error);
        this.placingOrder.set(false);
      },
      complete: () => this.placingOrder.set(false),
    });
  }

  canSell(): boolean {
    return (
      !this.placingOrder() &&
      !!this.selectedCommodity() &&
      !!this.selectedInventoryItem() &&
      !this.hasOwnBuyOrdersForSelectedItem() &&
      !this.hasOwnSellListingsForSelectedItem() &&
      !this.quantityCtrl.invalid &&
      !this.unitPriceCtrl.invalid &&
      (this.quantityCtrl.value ?? 0) > 0 &&
      (this.quantityCtrl.value ?? 0) <= this.ownedQuantity()
    );
  }

  canPlaceBuyOrder(): boolean {
    const total = this.buyOrderCommitment();

    return (
      !this.placingOrder() &&
      !!this.selectedCommodity() &&
      !this.hasOwnSellListingsForSelectedItem() &&
      !this.hasOwnBuyOrdersForSelectedItem() &&
      !this.quantityCtrl.invalid &&
      !this.unitPriceCtrl.invalid &&
      total > 0 &&
      total <= this.currentCinders()
    );
  }

  canSubmitBuyOrderAction(): boolean {
    return this.hasOwnOrderForSelectedItem()
      ? !this.placingOrder()
      : this.canPlaceBuyOrder();
  }

  canSubmitSellOrderAction(): boolean {
    return this.hasOwnOrderForSelectedItem()
      ? !this.placingOrder()
      : this.canSell();
  }

  canSubmitMobileOrder(): boolean {
    if (this.ticketSide() === 'sell') return this.canSubmitSellOrderAction();

    return this.mobileOrderBook() === 'buy'
      ? this.canSubmitBuyOrderAction()
      : this.canBuy();
  }

  canBuy(): boolean {
    return (
      !this.placingOrder() &&
      !!this.selectedCommodity() &&
      !this.selectedSellOrderIsOwn() &&
      !this.quantityCtrl.invalid &&
      !this.unitPriceCtrl.invalid &&
      (this.quantityCtrl.value ?? 0) > 0 &&
      this.availableAtOrderPrice() >= (this.quantityCtrl.value ?? 0) &&
      this.buyOrderCommitment() <= this.currentCinders()
    );
  }

  canFillBuyOrder(): boolean {
    return (
      !this.placingOrder() &&
      !!this.selectedCommodity() &&
      !!this.selectedInventoryItem() &&
      !this.quantityCtrl.invalid &&
      !this.unitPriceCtrl.invalid &&
      (this.quantityCtrl.value ?? 0) > 0 &&
      (this.quantityCtrl.value ?? 0) <= this.ownedQuantity() &&
      this.availableBuyOrderQuantity() >= (this.quantityCtrl.value ?? 0)
    );
  }

  trackCommodity = (_: number, commodity: Commodity) => commodity.base.id;
  trackOrderRow = (_: number, row: CommodityOrderRow) => row.unitPrice;
  trackBuyOrderRow = (_: number, row: CommodityBuyOrderRow) => row.unitPrice;

  private matchesCommodityType(
    item: MarketPlaceListing | InventoryItem,
  ): boolean {
    return this.matchesCommodityBase(item.itemInstance.itemBase);
  }

  private matchesCommodityBase(base: ItemBase): boolean {
    const subcategory = this._subcategory();
    const name = base.name.toLowerCase();
    const normalizedSubcategory = subcategory?.toLowerCase();

    return (
      base.itemType === this._itemType() &&
      base.stackable &&
      (!subcategory ||
        matchesMarketplaceResourceSubcategory(base, normalizedSubcategory) ||
        name === normalizedSubcategory)
    );
  }

  private resetMobileView(): void {
    this.mobileDetailOpen.set(false);
    this.mobileOrderBook.set('sell');
  }

  private availableAtOrderPrice(): number {
    const price = this.unitPriceCtrl.value;
    if (!price) return 0;

    return this.purchasableSellQuantityAtOrBelow(price);
  }

  private purchasableSellQuantityAtOrBelow(unitPrice: number): number {
    const characterId = this.currentCharacterId();
    return this.sellOrderRows()
      .filter((row) => row.unitPrice <= unitPrice)
      .flatMap((row) => row.listings)
      .filter((listing) => listing.sellerId !== characterId)
      .reduce((sum, listing) => sum + listing.quantity, 0);
  }

  private availableBuyOrderQuantity(): number {
    const minimumUnitPrice = this.unitPriceCtrl.value;
    if (!minimumUnitPrice) return 0;

    const characterId = this.currentCharacterId();
    return this.buyOrderRows()
      .filter((row) => row.unitPrice >= minimumUnitPrice)
      .flatMap((row) => row.orders)
      .filter((order) => order.buyerId !== characterId)
      .reduce((sum, order) => sum + order.quantity, 0);
  }
}
