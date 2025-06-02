import { CommonModule } from '@angular/common';
import { Component, computed, effect, OnInit, signal } from '@angular/core';
import { Tab } from '../../../../../shared/models/sidebar-item';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { TabComponent } from '../../../../../shared/components/tab/tab.component';
import { MarketPlaceInventoryItemComponent } from '../../../../../shared/components/market-place/market-place-inventory-item/market-place-inventory-item.component';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import {
  FormControl,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { RegularButtonComponent } from '../../../../../shared/components/buttons/regular-button/regular-button.component';
import {
  EquipmentInstance,
  EssenceItem,
} from '../../../../../shared/models/item';
import { MarketplaceStateService } from '../../../../../core/services/api/market-place/market-place-state.service';
import { MarketPlaceListing } from '../../../../../shared/models/Dtos/market-place/market-place-listing';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';

@Component({
  selector: 'app-market-place-sell',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TabComponent,
    MarketPlaceInventoryItemComponent,
    RegularButtonComponent,
    NumberFormatPipe,
  ],
  templateUrl: './market-place-sell.component.html',
  styleUrl: './market-place-sell.component.css',
})
export class MarketPlaceSellComponent implements OnInit {
  readonly myListings = signal<MarketPlaceListing[]>([]);

  readonly pendingItem = signal<InventoryItem | null>(null);
  selectedItemId: string = '';

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
        const mods = eq.attributeModifiers
          .map((m) => `• ${m.attributeType}: +${m.amount}`)
          .join('\n');
        return `Rarity: ${eq.rarity}\nType: ${eq.itemBase.equipmentType}\n${mods}`;
      }

      case 'Essence': {
        const es = base as EssenceItem;
        const mods = es.essence.attributeModifiers
          .map((m) => `• ${m.attributeType}: +${m.amount}`)
          .join('\n');
        return `Active:  ${es.essence.active.name}\nPassive: ${es.essence.passive.name}\n${mods}`;
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

  selectItem(item: InventoryItem) {
    this.pendingItem.set(item);
    this.selectedItemId = item.itemInstance.id;
  }

  listItem() {
    if (this.priceCtrl.invalid || this.qtyCtrl.invalid || !this.pendingItem())
      return;

    const qty = this.qtyCtrl.value!;
    const unitPrice = this.priceCtrl.value!;
    const item = this.pendingItem()!;

    this.marketplaceState
      .createListing(item, qty, unitPrice)
      .subscribe((listing) => {
        // remove or decrement from inventory
        if (item.itemInstance.itemBase.stackable && item.quantity > qty) {
          this.inventoryState.decrementItem(item.itemInstance.id, qty);
        } else {
          this.inventoryState.removeItem(item.itemInstance.id);
        }
        this.marketplaceState.addToListings(listing);

        this.pendingItem.set(null);
        this.priceCtrl.reset();
        this.qtyCtrl.reset();
      });
  }

  cancelListing(listing: MarketPlaceListing) {
    this.marketplaceState.cancelListing(listing.id).subscribe((success) => {
      const inventoryItem: InventoryItem = {
        id: crypto.randomUUID(),
        itemInstance: listing.itemInstance,
        quantity: listing.quantity,
      };
      this.inventoryState.add(inventoryItem);
      if (this.selectedItemId === inventoryItem.itemInstance.id)
        this.selectedItemId = '';
    });
  }

  tabs: Tab[] = [
    {
      label: 'All',
      items: [],
    },
    {
      label: 'Equipment',
      items: [],
    },
    {
      label: 'Resources',
      items: [],
    },
    {
      label: 'Essences',
      items: [],
    },
  ];

  activeTab: string = '';

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }

  get filteredItems(): InventoryItem[] {
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

  trackByItem = (_: number, item: InventoryItem) => item.id;
  // trackByListing = (_: number, l: Listing) => l.id;
  trackByListing = (_: number, l: MarketPlaceListing) => l.id;
}
