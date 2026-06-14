import { CommonModule } from '@angular/common';
import {
  Component,
  computed,
  effect,
  Input,
  OnInit,
  signal,
} from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { concatMap, from, tap } from 'rxjs';

import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { MarketplaceStateService } from '../../../../../core/services/api/market-place/market-place-state.service';
import { RegularButtonComponent } from '../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { MarketPlaceListing } from '../../../../../shared/models/Dtos/market-place/market-place-listing';
import { ItemBase } from '../../../../../shared/models/item';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';

interface Commodity {
  base: ItemBase;
  ownedQuantity: number;
  listedQuantity: number;
  bestSellPrice: number | null;
}

interface CommodityOrderRow {
  unitPrice: number;
  quantity: number;
  listings: MarketPlaceListing[];
}

@Component({
  selector: 'app-market-place-commodity',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RegularButtonComponent,
    NumberFormatPipe,
  ],
  templateUrl: './market-place-commodity.component.html',
})
export class MarketPlaceCommodityComponent implements OnInit {
  private readonly _itemType = signal<ItemType>(ItemType.Resource);
  private readonly _subcategory = signal<string | null>(null);

  @Input({ required: true })
  set itemType(value: ItemType) {
    this._itemType.set(value);
  }

  @Input()
  set subcategory(value: string | null) {
    this._subcategory.set(value);
  }

  readonly selectedSellPrice = signal<number | null>(null);
  readonly placingOrder = signal(false);

  readonly quantityCtrl = new FormControl<number>(1, {
    validators: [Validators.required, Validators.min(1)],
  });
  readonly unitPriceCtrl = new FormControl<number | null>(null, {
    validators: [Validators.required, Validators.min(1)],
  });

  readonly listings = computed(() =>
    this.marketplaceState
      .listings()
      .filter((listing) => this.matchesCommodityType(listing)),
  );

  readonly inventory = computed(() =>
    this.inventoryState
      .items()
      .filter((item) => this.matchesCommodityType(item)),
  );

  readonly commodities = computed(() => {
    const byBase = new Map<string, Commodity>();

    for (const item of this.inventory()) {
      const base = item.itemInstance.itemBase;
      const existing = byBase.get(base.id);
      byBase.set(base.id, {
        base,
        ownedQuantity: (existing?.ownedQuantity ?? 0) + item.quantity,
        listedQuantity: existing?.listedQuantity ?? 0,
        bestSellPrice: existing?.bestSellPrice ?? null,
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
        bestSellPrice:
          best === null ? listing.unitPrice : Math.min(best, listing.unitPrice),
      });
    }

    return [...byBase.values()].sort((a, b) =>
      a.base.name.localeCompare(b.base.name),
    );
  });

  readonly marketName = computed(() => {
    const commodities = this.commodities();
    if (commodities.length === 1) return commodities[0].base.name;
    return this._subcategory() || this._itemType();
  });

  readonly hasSingleCommodity = computed(() => this.commodities().length === 1);

  readonly selectedInventoryItem = computed(() => {
    if (!this.hasSingleCommodity()) return null;
    return this.inventory()[0] ?? null;
  });

  readonly sellOrderRows = computed(() => {
    const grouped = new Map<number, CommodityOrderRow>();
    for (const listing of this.listings()) {
      const existing = grouped.get(listing.unitPrice);
      if (existing) {
        existing.quantity += listing.quantity;
        existing.listings.push(listing);
      } else {
        grouped.set(listing.unitPrice, {
          unitPrice: listing.unitPrice,
          quantity: listing.quantity,
          listings: [listing],
        });
      }
    }

    return [...grouped.values()].sort((a, b) => a.unitPrice - b.unitPrice);
  });

  readonly ownedQuantity = computed(() =>
    this.commodities().reduce(
      (total, commodity) => total + commodity.ownedQuantity,
      0,
    ),
  );

  readonly bestSellPrice = computed(() => {
    const prices = this.commodities()
      .map((commodity) => commodity.bestSellPrice)
      .filter((price): price is number => price !== null);
    return prices.length ? Math.min(...prices) : null;
  });

  constructor(
    private readonly inventoryState: InventoryStateService,
    private readonly marketplaceState: MarketplaceStateService,
  ) {
    effect(
      () => {
        this._itemType();
        this._subcategory();
        this.quantityCtrl.setValue(1, { emitEvent: false });
        this.unitPriceCtrl.setValue(this.bestSellPrice(), {
          emitEvent: false,
        });
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
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
          this.selectSellOrder(firstSellOrder);
        }
      },
      { allowSignalWrites: true },
    );
  }

  ngOnInit(): void {
    this.inventoryState.load();
    this.marketplaceState.load();
  }

  selectSellOrder(row: CommodityOrderRow): void {
    this.selectedSellPrice.set(row.unitPrice);
    this.unitPriceCtrl.setValue(row.unitPrice, { emitEvent: false });
  }

  sellSelectedCommodity(): void {
    const item = this.selectedInventoryItem();
    if (!item || this.quantityCtrl.invalid || this.unitPriceCtrl.invalid)
      return;

    const quantity = this.quantityCtrl.value!;
    const unitPrice = this.unitPriceCtrl.value!;
    if (quantity > this.ownedQuantity()) return;

    this.placingOrder.set(true);
    this.marketplaceState.createListing(item, quantity, unitPrice).subscribe({
      next: (listing) => {
        if (item.itemInstance.itemBase.stackable && item.quantity > quantity) {
          this.inventoryState.decrementItem(item.itemInstance.id, quantity);
        } else {
          this.inventoryState.removeItem(item.itemInstance.id);
        }
        this.marketplaceState.addToListings(listing);
      },
      error: (error) => {
        console.error(error);
        this.placingOrder.set(false);
      },
      complete: () => this.placingOrder.set(false),
    });
  }

  buySelectedCommodity(): void {
    if (this.quantityCtrl.invalid || this.unitPriceCtrl.invalid) return;

    let remaining = this.quantityCtrl.value!;
    const maxUnitPrice = this.unitPriceCtrl.value!;
    const plan = this.sellOrderRows()
      .filter((row) => row.unitPrice <= maxUnitPrice)
      .flatMap((row) =>
        row.listings
          .slice()
          .sort((a, b) =>
            a.createdAt.toString().localeCompare(b.createdAt.toString()),
          ),
      )
      .map((listing) => {
        const quantity = Math.min(remaining, listing.quantity);
        remaining -= quantity;
        return { listing, quantity };
      })
      .filter((purchase) => purchase.quantity > 0);

    if (remaining > 0 || plan.length === 0) return;

    this.placingOrder.set(true);
    from(plan)
      .pipe(
        concatMap((purchase) =>
          this.marketplaceState
            .buyoutListing(purchase.listing.id, purchase.quantity)
            .pipe(
              tap(() =>
                this.marketplaceState.decrementListing(
                  purchase.listing.id,
                  purchase.quantity,
                ),
              ),
            ),
        ),
      )
      .subscribe({
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
      this.hasSingleCommodity() &&
      !!this.selectedInventoryItem() &&
      !this.quantityCtrl.invalid &&
      !this.unitPriceCtrl.invalid &&
      (this.quantityCtrl.value ?? 0) <= this.ownedQuantity()
    );
  }

  canBuy(): boolean {
    return (
      !this.placingOrder() &&
      this.hasSingleCommodity() &&
      !this.quantityCtrl.invalid &&
      !this.unitPriceCtrl.invalid &&
      (this.quantityCtrl.value ?? 0) > 0 &&
      this.availableAtOrderPrice() >= (this.quantityCtrl.value ?? 0)
    );
  }

  trackCommodity = (_: number, commodity: Commodity) => commodity.base.id;
  trackOrderRow = (_: number, row: CommodityOrderRow) => row.unitPrice;

  private matchesCommodityType(
    item: MarketPlaceListing | InventoryItem,
  ): boolean {
    const base = item.itemInstance.itemBase;
    const subcategory = this._subcategory();
    const category = item.itemInstance.category?.toLowerCase();
    const name = base.name.toLowerCase();

    return (
      base.itemType === this._itemType() &&
      base.stackable &&
      (!subcategory ||
        category === subcategory.toLowerCase() ||
        name === subcategory.toLowerCase())
    );
  }

  private availableAtOrderPrice(): number {
    const price = this.unitPriceCtrl.value;
    if (!price) return 0;
    return this.sellOrderRows()
      .filter((row) => row.unitPrice <= price)
      .reduce((sum, row) => sum + row.quantity, 0);
  }
}
