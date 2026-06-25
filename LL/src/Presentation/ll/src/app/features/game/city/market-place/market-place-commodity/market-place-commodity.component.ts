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
import { concatMap, from } from 'rxjs';

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
  private readonly resourceFamilyItemIds = new Map<string, string[]>([
    ['metal', ['ore']],
    ['wood', ['wood']],
    ['hide', ['rawhide']],
    ['crystal', ['crystalline_powder']],
    ['stone', ['rough_stone']],
    ['fiber', ['woven_fiber']],
    ['bone', ['bone_fragments']],
    ['chitin', ['ant_chitin']],
    ['resin', ['hive_resin']],
    ['oil', ['murky_fish_oil']],
  ]);
  private readonly specialMaterialItemIds = new Set([
    'venom_gland',
    'royal_chitin_plate',
    'hive_ichor',
  ]);
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
  readonly selectedCommodityId = signal<string | null>(null);
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

    return [...byBase.values()].sort((a, b) => {
      const blueprintRank =
        Number(this.isBlueprintResource(b.base)) -
        Number(this.isBlueprintResource(a.base));
      if (blueprintRank !== 0) return blueprintRank;

      const tierRank =
        this.getResourceFamilySortRank(a.base) -
        this.getResourceFamilySortRank(b.base);
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

  readonly selectedListings = computed(() => {
    const selected = this.selectedCommodity();
    if (!selected) return [];

    return this.listings().filter(
      (listing) => listing.itemInstance.itemBase.id === selected.base.id,
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
    for (const listing of this.selectedListings()) {
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
    this.selectedCommodity()?.ownedQuantity ?? 0,
  );

  readonly bestSellPrice = computed(() => {
    return this.selectedCommodity()?.bestSellPrice ?? null;
  });

  constructor(
    private readonly inventoryState: InventoryStateService,
    private readonly marketplaceState: MarketplaceStateService,
  ) {
    effect(
      () => {
        this._itemType();
        this._subcategory();
        this.selectedCommodityId();
        this.quantityCtrl.setValue(1, { emitEvent: false });
        this.unitPriceCtrl.setValue(this.bestSellPrice(), {
          emitEvent: false,
        });
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
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
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
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

  selectCommodity(commodity: Commodity): void {
    this.selectedCommodityId.set(commodity.base.id);
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
          this.marketplaceState.buyoutListing(
            purchase.listing.id,
            purchase.quantity,
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
      !!this.selectedCommodity() &&
      !!this.selectedInventoryItem() &&
      !this.quantityCtrl.invalid &&
      !this.unitPriceCtrl.invalid &&
      (this.quantityCtrl.value ?? 0) <= this.ownedQuantity()
    );
  }

  canBuy(): boolean {
    return (
      !this.placingOrder() &&
      !!this.selectedCommodity() &&
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
    const normalizedSubcategory = subcategory?.toLowerCase();

    return (
      base.itemType === this._itemType() &&
      base.stackable &&
      (!subcategory ||
        this.matchesResourceGroup(base, normalizedSubcategory) ||
        category === normalizedSubcategory ||
        name === normalizedSubcategory)
    );
  }

  private matchesResourceGroup(
    base: ItemBase,
    subcategory: string | undefined,
  ): boolean {
    if (base.itemType !== ItemType.Resource || !subcategory) return false;

    switch (subcategory) {
      case 'blueprints':
        return this.isBlueprintResource(base);
      case 'catalysts':
        return this.specialMaterialItemIds.has(base.id);
      default:
        return this.resourceFamilyItemIds.get(subcategory)?.includes(base.id) ??
          false;
    }
  }

  private getResourceFamilySortRank(base: ItemBase): number {
    const subcategory = this._subcategory()?.toLowerCase();
    if (!subcategory) return Number.MAX_SAFE_INTEGER;

    const familyIds =
      subcategory === 'catalysts'
        ? [...this.specialMaterialItemIds]
        : this.resourceFamilyItemIds.get(subcategory);
    const index = familyIds?.indexOf(base.id) ?? -1;

    return index === -1 ? Number.MAX_SAFE_INTEGER : index;
  }

  private isBlueprintResource(base: ItemBase): boolean {
    return (
      base.itemType === ItemType.Resource &&
      (base.id.toLowerCase().startsWith('blueprint_') ||
        base.name.toLowerCase().startsWith('blueprint:'))
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
