import { CommonModule } from '@angular/common';
import {
  Component,
  computed,
  effect,
  Input,
  OnInit,
  signal,
} from '@angular/core';
import { SidebarSection } from '../../../../../shared/models/sidebar-item';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { MarketPlaceInventoryItemComponent } from '../../../../../shared/components/market-place/market-place-inventory-item/market-place-inventory-item.component';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import {
  FormControl,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { RegularButtonComponent } from '../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import {
  EquipmentInstance,
  EssenceItem,
  ItemBase,
} from '../../../../../shared/models/item';
import { MarketplaceStateService } from '../../../../../core/services/api/market-place/market-place-state.service';
import { MarketPlaceListing } from '../../../../../shared/models/Dtos/market-place/market-place-listing';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';
import { FilterTabsComponent } from '../../../../../shared/components/custom-components/tabs/filter-tabs/filter-tabs.component';
import { EquipmentTypePipe } from '../../../../../shared/pipes/equipment/equipment-type-format/equipment-type.pipe';
import { ItemComponent } from '../../../../../shared/components/item/item.component';
import { formatAttributeType } from '../../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { formatAttributeValue } from '../../../../../shared/pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import { ItemQuality } from '../../../../../shared/models/enums/itemQuality';
import { MarketCategoryId } from '../../../../../shared/models/market-category';
import {
  isMarketplaceBlueprintResource,
  isMarketplaceTradableItemBase,
  MARKETPLACE_CATALYST_ITEM_IDS,
  matchesMarketplaceResourceSubcategory,
} from '../../../../../shared/utils/market-place/market-place-category.utils';
import { aggregateAttributes } from '../../../../../shared/utils/attributes/attribute-order.utils';

@Component({
  selector: 'app-market-place-sell',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MarketPlaceInventoryItemComponent,
    RegularButtonComponent,
    NumberFormatPipe,
    FilterTabsComponent,
    ItemComponent,
  ],
  templateUrl: './market-place-sell.component.html',
})
export class MarketPlaceSellComponent implements OnInit {
  readonly myListings = signal<MarketPlaceListing[]>([]);
  readonly selectedItemType = signal<ItemType | null>(null);
  readonly selectedCategory = signal<MarketCategoryId>('resources');
  readonly selectedSubcategory = signal<string | null>(null);

  readonly pendingItem = signal<InventoryItem | null>(null);
  selectedItemId: string = '';

  @Input()
  set itemType(value: ItemType | null) {
    this.selectedItemType.set(value);
    this.pendingItem.set(null);
    this.selectedItemId = '';
  }

  @Input({ required: true })
  set category(value: MarketCategoryId) {
    this.selectedCategory.set(value);
    this.pendingItem.set(null);
    this.selectedItemId = '';
  }

  @Input()
  set subcategory(value: string | null) {
    this.selectedSubcategory.set(value);
    this.pendingItem.set(null);
    this.selectedItemId = '';
  }

  /** Price input */
  readonly priceCtrl = new FormControl<number | null>(null, {
    validators: [Validators.required, Validators.min(1)],
  });
  readonly qtyCtrl = new FormControl<number>(1, {
    validators: [Validators.required, Validators.min(1)],
  });

  constructor(
    private readonly inventoryState: InventoryStateService,
    private readonly marketplaceState: MarketplaceStateService,
  ) {
    this.inventoryState.load();
    this.marketplaceState.load();
    effect(() => {
      const pi = this.pendingItem();
      if (!pi) return;

      const max = this.maxQuantity();
      this.qtyCtrl.setValidators([
        Validators.required,
        Validators.min(1),
        Validators.max(max),
      ]);
      this.qtyCtrl.setValue(1, { emitEvent: false });
      this.qtyCtrl.updateValueAndValidity({ emitEvent: false });
    });

    effect(
      () => {
        this.myListings.set(this.marketplaceState.myListings());
      },
      { allowSignalWrites: true }, // ✅ Add this option
    );

    this.setActiveTab(this.tabs[0]?.label || '');
  }

  ngOnInit(): void {
    this.qtyCtrl.valueChanges.subscribe((raw) => {
      // value that just came from the input – make sure it is a number
      if (!raw) return;
      const value = +raw || 0;

      const max = this.maxQuantity(); // your helper
      const min = 1;

      // If outside the allowed range, immediately push back a legal value.
      if (value > max) {
        this.qtyCtrl.setValue(max, { emitEvent: false }); // cap top
      } else if (value < min) {
        this.qtyCtrl.setValue(min, { emitEvent: false }); // cap bottom (optional)
      }
    });
  }

  readonly itemDescription = computed(() => {
    const pi = this.pendingItem();
    if (!pi) return '';

    const base = pi.itemInstance.itemBase;
    const instance = pi.itemInstance;

    switch (base.itemType) {
      case 'Equipment': {
        const eq = instance as EquipmentInstance;
        const mods = aggregateAttributes(eq.attributeModifiers)
          .map(
            (m) =>
              `• ${formatAttributeType(m.attributeType, true)}: ${formatAttributeValue(m.amount, m.attributeType, true, true)}`,
          )
          .join('\n');
        return `Rarity: ${eq.rarity}\nType: ${new EquipmentTypePipe().transform(eq.equipmentBase.equipmentType)}\n${mods}`;
      }

      case 'Essence': {
        const es = base as EssenceItem;
        return `${es.rarity}${es.description ? `\n${es.description}` : ''}`;
      }

      default:
        /* Materials or other stackables */
        return `${base.rarity}\n${base.description}`;
    }
  });

  readonly maxQuantity = () => {
    const pi = this.pendingItem();
    if (!pi) return 1;
    return pi.itemInstance.itemBase.stackable ? pi.quantity : 1;
  };

  readonly grossTotal = () =>
    (this.priceCtrl.value ?? 0) * (this.qtyCtrl.value ?? 0);

  readonly estimatedFee = () =>
    this.grossTotal() > 0
      ? Math.max(1, Math.ceil(this.grossTotal() * 0.03))
      : 0;

  readonly estimatedProceeds = () =>
    Math.max(0, this.grossTotal() - this.estimatedFee());

  readonly displayedMyListings = computed(() => {
    const itemType = this.selectedItemType();
    if (!itemType) return this.myListings();
    return this.myListings().filter(
      (listing) =>
        listing.itemInstance.itemBase.itemType === itemType &&
        this.matchesSelectedCategory(listing.itemInstance.itemBase),
    );
  });

  readonly matchingBuyOrders = computed(() => {
    const pending = this.pendingItem();
    if (!pending?.itemInstance.itemBase.stackable) return [];

    const ownOrderIds = new Set(
      this.marketplaceState.myBuyOrders().map((order) => order.id),
    );
    return this.marketplaceState
      .buyOrders()
      .filter(
        (order) =>
          order.itemBaseId === pending.itemInstance.itemBase.id &&
          !ownOrderIds.has(order.id),
      )
      .sort(
        (a, b) =>
          b.unitPrice - a.unitPrice ||
          a.createdAt.toString().localeCompare(b.createdAt.toString()),
      );
  });

  readonly bestBuyOrderPrice = computed(
    () => this.matchingBuyOrders()[0]?.unitPrice ?? null,
  );

  readonly hasOwnBuyOrderForPendingItem = computed(() => {
    const itemBaseId = this.pendingItem()?.itemInstance.itemBase.id;
    if (!itemBaseId) return false;

    return this.marketplaceState
      .myBuyOrders()
      .some((order) => order.itemBaseId === itemBaseId);
  });

  readonly hasOwnSellListingForPendingItem = computed(() => {
    const base = this.pendingItem()?.itemInstance.itemBase;
    if (!base?.stackable) return false;

    return this.marketplaceState
      .myListings()
      .some((listing) => listing.itemInstance.itemBase.id === base.id);
  });

  selectItem(item: InventoryItem) {
    if (!isMarketplaceTradableItemBase(item.itemInstance.itemBase)) return;

    this.pendingItem.set(item);
    this.selectedItemId = item.itemInstance.id;
    this.priceCtrl.setValue(this.bestBuyOrderPrice(), { emitEvent: false });
  }

  listItem() {
    if (!this.canCreateListing()) return;

    const qty = this.qtyCtrl.value!;
    const unitPrice = this.priceCtrl.value!;
    const item = this.pendingItem()!;

    this.marketplaceState.createListing(item, qty, unitPrice).subscribe(() => {
      this.pendingItem.set(null);
      this.selectedItemId = '';
      this.priceCtrl.reset();
      this.qtyCtrl.reset();
    });
  }

  sellNow(): void {
    const item = this.pendingItem();
    if (!item || !this.canSellNow()) return;

    this.marketplaceState
      .sellCommodity(
        item.itemInstance.id,
        this.qtyCtrl.value!,
        this.priceCtrl.value!,
      )
      .subscribe(() => {
        this.pendingItem.set(null);
        this.selectedItemId = '';
        this.priceCtrl.reset();
        this.qtyCtrl.setValue(1);
      });
  }

  canSellNow(): boolean {
    const pending = this.pendingItem();
    const quantity = this.qtyCtrl.value ?? 0;
    const minimumPrice = this.priceCtrl.value ?? 0;
    if (
      !pending?.itemInstance.itemBase.stackable ||
      !isMarketplaceTradableItemBase(pending.itemInstance.itemBase) ||
      this.qtyCtrl.invalid ||
      this.priceCtrl.invalid
    )
      return false;

    const demand = this.matchingBuyOrders()
      .filter((order) => order.unitPrice >= minimumPrice)
      .reduce((sum, order) => sum + order.quantity, 0);
    return quantity > 0 && demand >= quantity;
  }

  canCreateListing(): boolean {
    const pending = this.pendingItem();
    return (
      !!pending &&
      isMarketplaceTradableItemBase(pending.itemInstance.itemBase) &&
      !this.hasOwnBuyOrderForPendingItem() &&
      !this.hasOwnSellListingForPendingItem() &&
      !this.priceCtrl.invalid &&
      !this.qtyCtrl.invalid
    );
  }

  cancelListing(listing: MarketPlaceListing) {
    this.marketplaceState.cancelListing(listing.id).subscribe((response) => {
      if (this.selectedItemId === response.returnedItem.itemInstance.id) {
        this.selectedItemId = '';
      }
    });
  }

  listingQuality(listing: MarketPlaceListing): ItemQuality | null {
    return listing.itemInstance.itemBase.itemType === ItemType.Equipment
      ? (listing.itemInstance as EquipmentInstance).quality
      : null;
  }

  tabs: SidebarSection[] = [
    {
      id: 'all',
      label: 'All',
      items: [],
    },
    {
      id: 'equipment',
      label: 'Equipment',
      items: [],
    },
    {
      id: 'resources',
      label: 'Resources',
      items: [],
    },
    {
      id: 'essences',
      label: 'Essences',
      items: [],
    },
  ];

  activeTab: string = '';

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }

  get filteredItems(): InventoryItem[] {
    let items: InventoryItem[];

    switch (this.selectedItemType()) {
      case ItemType.Equipment:
        items = this.inventoryState.equipment();
        break;

      case ItemType.Essence:
        items = this.inventoryState.essences();
        break;

      case ItemType.Resource:
        items = this.inventoryState.materials();
        break;

      default:
        items = this.itemsForActiveTab();
        break;
    }

    return items.filter(
      (item) =>
        isMarketplaceTradableItemBase(item.itemInstance.itemBase) &&
        this.matchesSelectedCategory(item.itemInstance.itemBase),
    );
  }

  private itemsForActiveTab(): InventoryItem[] {
    switch (this.activeTab) {
      case 'All':
        return this.inventoryState.items();

      case 'Equipment':
        return this.inventoryState.equipment();

      case 'Resources':
        return this.inventoryState.materials();

      case 'Essences':
        return this.inventoryState.essences();

      default:
        return this.inventoryState.items();
    }
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }

  get inventoryTitle(): string {
    if (this.selectedCategory() === 'blueprints') return 'Blueprints';
    if (this.selectedCategory() === 'catalysts') return 'Catalysts';

    switch (this.selectedItemType()) {
      case ItemType.Equipment:
        return 'Equipment';

      case ItemType.Essence:
        return 'Essences';

      default:
        return 'Inventory';
    }
  }

  private matchesSelectedCategory(base: ItemBase): boolean {
    if (this.selectedCategory() === 'blueprints') {
      return isMarketplaceBlueprintResource(base);
    }

    if (this.selectedCategory() === 'catalysts') {
      return MARKETPLACE_CATALYST_ITEM_IDS.has(base.id);
    }

    if (this.selectedCategory() === 'resources') {
      return matchesMarketplaceResourceSubcategory(
        base,
        this.selectedSubcategory(),
      );
    }

    return true;
  }

  trackByItem = (_: number, item: InventoryItem) => item.id;
  // trackByListing = (_: number, l: Listing) => l.id;
  trackByListing = (_: number, l: MarketPlaceListing) => l.id;
}
