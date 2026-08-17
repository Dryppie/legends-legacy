import {
  DecimalPipe,
  NgClass,
  NgFor,
  NgIf,
  NgSwitch,
  NgSwitchCase,
  NgSwitchDefault,
} from '@angular/common';
import {
  Component,
  computed,
  effect,
  Input,
  OnInit,
  signal,
} from '@angular/core';
import {
  FormControl,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { RegularButtonComponent } from '../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { MarketPlaceListing } from '../../../../../shared/models/Dtos/market-place/market-place-listing';
import { MarketplaceStateService } from '../../../../../core/services/api/market-place/market-place-state.service';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';
import { MarketPlaceListingItemComponent } from '../../../../../shared/components/market-place/market-place-listing-item/market-place-listing-item.component';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import {
  EquipmentInstance,
  EssenceItem,
  essenceItemToEssence,
} from '../../../../../shared/models/item';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  debounceTime,
  distinctUntilChanged,
  map,
  startWith,
} from 'rxjs/operators';
import { EquipmentTypePipe } from '../../../../../shared/pipes/equipment/equipment-type-format/equipment-type.pipe';
import { ItemComponent } from '../../../../../shared/components/item/item.component';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { EquipmentStateService } from '../../../../../core/services/api/equipment/equipment-state.service';
import { EquipmentSlotType } from '../../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { getSlotTypeFromEquipmentType } from '../../../../../shared/utils/equipment/equipment.utils';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { EquipmentType } from '../../../../../shared/models/enums/equipmentType';
import { ItemQuality } from '../../../../../shared/models/enums/itemQuality';
import { AttributeModifier } from '../../../../../shared/models/Dtos/attributesDto';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';
import {
  AttributeTypeFormatPipe,
  formatAttributeType,
} from '../../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import {
  AttributeValueFormatPipe,
  formatAttributeValue,
} from '../../../../../shared/pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../../../../shared/components/custom-components/dropdown/dropdown.component';
import { AttributeDisplayPipe } from '../../../../../shared/pipes/attributes/attribute-display/attribute-display.pipe';
import { aggregateAttributes } from '../../../../../shared/utils/attributes/attribute-order.utils';
import { AttributeTooltipDirective } from '../../../../../shared/directives/attribute-tooltip/attribute-tooltip.directive';
import { marketplaceCommoditySearchText } from '../market-place-commodity/market-place-commodity-search';

@Component({
    selector: 'app-market-place-buy',
    imports: [
        NgFor,
        NgIf,
        NgClass,
        FormsModule,
        ReactiveFormsModule,
        RegularButtonComponent,
        NumberFormatPipe,
        MarketPlaceListingItemComponent,
        NgSwitch,
        NgSwitchCase,
        NgSwitchDefault,
        ItemComponent,
        DecimalPipe,
        AttributeTypeFormatPipe,
        AttributeValueFormatPipe,
        AttributeDisplayPipe,
        AttributeTooltipDirective,
        DropdownComponent,
    ],
    templateUrl: './market-place-buy.component.html'
})
export class MarketPlaceBuyComponent implements OnInit {
  readonly allListings = signal<MarketPlaceListing[]>([]);
  readonly selectedItemType = signal<ItemType | null>(null);
  selectedListingId: string = '';

  @Input()
  set itemType(value: ItemType | null) {
    this.selectedItemType.set(value);
    this.selectedListing.set(null);
    this.selectedListingId = '';
  }

  readonly qtyCtrl = new FormControl<number>(1, {
    validators: [Validators.required, Validators.min(1)],
  });

  /** UI filters */
  readonly searchCtrl = new FormControl<string>('', {
    nonNullable: true,
    validators: [Validators.maxLength(64)],
  });

  readonly searchTerm = toSignal(
    this.searchCtrl.valueChanges.pipe(
      startWith(this.searchCtrl.value), // first value immediately
      debounceTime(300), // wait until user pauses
      map((v) => v.trim().toLowerCase()),
      distinctUntilChanged(),
    ),
    { initialValue: '' },
  );

  readonly rarity = signal<string>('');
  readonly equipmentType = signal<string>('');
  readonly quality = signal<string>('');
  readonly priceSort = signal<'' | 'asc' | 'desc'>('');
  readonly minimumTierCtrl = new FormControl<number | null>(null, {
    validators: [Validators.min(1)],
  });
  readonly minimumPotentialCtrl = new FormControl<number | null>(null, {
    validators: [Validators.min(0)],
  });
  readonly minimumTier = toSignal(
    this.minimumTierCtrl.valueChanges.pipe(startWith(this.minimumTierCtrl.value)),
    { initialValue: null },
  );
  readonly minimumPotential = toSignal(
    this.minimumPotentialCtrl.valueChanges.pipe(
      startWith(this.minimumPotentialCtrl.value),
    ),
    { initialValue: null },
  );

  readonly rarities = [
    'Common',
    'Uncommon',
    'Rare',
    'Epic',
    'Unique',
    'Legendary',
    'Legacy',
  ];
  readonly rarityOptions: DropdownOption<string>[] = [
    { label: 'Any rarity', value: '' },
    ...this.rarities.map((rarity) => ({ label: rarity, value: rarity })),
  ];
  readonly equipmentTypeOptions: DropdownOption<string>[] = [
    { label: 'Any slot', value: '' },
    ...Object.values(EquipmentType).map((value) => ({
      label: new EquipmentTypePipe().transform(value),
      value,
    })),
  ];
  readonly qualityOptions: DropdownOption<string>[] = [
    { label: 'Any quality', value: '' },
    ...Object.values(ItemQuality).map((value) => ({ label: value, value })),
  ];

  /** Selected listing shown inside confirmation modal */
  readonly selectedListing = signal<MarketPlaceListing | null>(null);

  readonly filteredListings = computed(() => {
    let items = [...this.allListings()];

    const q = this.searchTerm();
    if (q) {
      items = items.filter((l) =>
        [
          l.itemInstance.displayName ?? '',
          marketplaceCommoditySearchText(l.itemInstance.itemBase),
        ]
          .join(' ')
          .toLowerCase()
          .includes(q),
      );
    }

    const itemTypeFilter = this.selectedItemType();
    if (itemTypeFilter) {
      items = items.filter(
        (l) => l.itemInstance.itemBase.itemType === itemTypeFilter,
      );
    }

    if (this.rarity()) {
      items = items.filter((l) => {
        if (l.itemInstance.itemBase.itemType === ItemType.Equipment) {
          return (l.itemInstance as EquipmentInstance).rarity === this.rarity();
        }
        return l.itemInstance.itemBase.rarity === this.rarity();
      });
    }

    if (this.selectedItemType() === ItemType.Equipment) {
      if (this.equipmentType()) {
        items = items.filter(
          (listing) =>
            (listing.itemInstance as EquipmentInstance).equipmentBase
              .equipmentType === this.equipmentType(),
        );
      }

      if (this.quality()) {
        items = items.filter(
          (listing) =>
            (listing.itemInstance as EquipmentInstance).quality === this.quality(),
        );
      }

      const minimumTier = this.minimumTier();
      if (minimumTier !== null) {
        items = items.filter(
          (listing) =>
            (listing.itemInstance as EquipmentInstance).tier >= minimumTier,
        );
      }

      const minimumPotential = this.minimumPotential();
      if (minimumPotential !== null) {
        items = items.filter(
          (listing) =>
            ((listing.itemInstance as EquipmentInstance).potential ?? 0) >=
            minimumPotential,
        );
      }
    }

    /* 4️⃣ Sort */
    if (this.priceSort()) {
      items.sort((a, b) =>
        this.priceSort() === 'asc'
          ? a.unitPrice - b.unitPrice
          : b.unitPrice - a.unitPrice,
      );
    }

    return items;
  });

  constructor(
    private readonly marketplaceState: MarketplaceStateService,
    private readonly inventoryState: InventoryStateService,
    private readonly equipmentState: EquipmentStateService,
    public readonly characterState: CharacterStateService,
  ) {
    this.inventoryState.load();
    this.equipmentState.load();
    /* Keep our local copy of listings in sync with the store */
    effect(
      () => {
        this.allListings.set(this.marketplaceState.listings());
      },
      { allowSignalWrites: true }, // ✅ Add this option
    );

    effect(() => {
      const pi = this.selectedListing();
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
  }

  ngOnInit(): void {
    /* Load remote data once */
    this.marketplaceState.load();

    /* Bridge reactive form control → signal */
    this.searchCtrl.valueChanges.subscribe((v) => this.applySearch(v));
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
    const listing = this.selectedListing();
    if (!listing) return '';

    const base = listing.itemInstance.itemBase;
    const instance = listing.itemInstance;

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

  getAttributeModifiers(e: InventoryItem): AttributeModifier[] {
    if (e.itemInstance.itemBase.itemType === ItemType.Equipment) {
      return (e.itemInstance as EquipmentInstance).attributeModifiers;
    }
    return [];
  }

  readonly selectedListingAsEquipment = computed(() => {
    const listing = this.selectedListing();
    if (!listing) return null;

    if (listing.itemInstance.itemBase.itemType === ItemType.Equipment) {
      const instance = listing.itemInstance;
      const equippedItems: InventoryItem[] = [];

      const eq = instance as EquipmentInstance;
      const primarySlot = getSlotTypeFromEquipmentType(
        eq.equipmentBase.equipmentType,
      );
      const affectedSlots: EquipmentSlotType[] = [primarySlot];

      //If currently selected is a two-handed, add the off-hand as well
      if (eq.equipmentBase.equipmentType === EquipmentType.TwoHanded) {
        affectedSlots.push(EquipmentSlotType.OffHand);
      }

      for (const slot of affectedSlots) {
        const equipped = this.equipmentState.getSlot(slot);
        if (equipped?.equipmentInstance) {
          equippedItems.push({
            id: equipped.equipmentInstance.id,
            itemInstance: equipped.equipmentInstance,
            quantity: 1,
          });
        }
        //if the equipped piece is a two-handed weapon we break early to avoid showing the equipment piece twice
        if (
          equipped?.equipmentInstance?.equipmentBase.equipmentType ===
          EquipmentType.TwoHanded
        ) {
          break;
        }
      }

      return equippedItems;
    }
    return null;
  });

  readonly selectedListingAsMaterial = computed(() => {
    const listing = this.selectedListing();
    if (!listing) return null;
    if (listing.itemInstance.itemBase.itemType === ItemType.Resource) {
      const items: InventoryItem[] = [];

      const listingBaseId = listing.itemInstance.itemBase.id;
      const inventory = this.inventoryState.items();

      for (const invItem of inventory) {
        if (invItem.itemInstance.itemBase.id === listingBaseId) {
          items.push(invItem);
        }
      }
      return items;
    }
    return null;
  });

  buyoutListing(): void {
    const sel = this.selectedListing();
    if (!sel || this.isOwnListing(sel) || this.qtyCtrl.invalid) return;
    const qty = this.qtyCtrl.value!;

    // 👇 Replace with your real service / API call
    this.marketplaceState.buyoutListing(sel.id, qty).subscribe({
      next: (response) => {
        if (response.remainingListing) {
          this.selectListing(response.remainingListing);
        } else {
          this.selectedListing.set(null);
          this.selectedListingId = '';
        }
      },
      error: (err) => {
        // Handle error (e.g., show toast)
        console.error(err);
      },
    });
  }

  readonly maxQuantity = () => {
    const listing = this.selectedListing();
    if (!listing) return 1;
    return listing.itemInstance.itemBase.stackable ? listing.quantity : 1;
  };

  applySearch(value: string) {
    /* No debounce here – leave that to the template via input event if needed */
    /* Normalise: signal expects lowercase */
    // Nothing extra – the computed already consumes searchCtrl.value directly.
  }

  togglePriceSort() {
    this.priceSort.update(
      (cur) => (cur === '' ? 'asc' : cur === 'asc' ? 'desc' : ''), // cur === 'desc'
    );
  }

  resetFilters() {
    this.searchCtrl.reset();
    this.rarity.set('');
    this.equipmentType.set('');
    this.quality.set('');
    this.minimumTierCtrl.reset();
    this.minimumPotentialCtrl.reset();
    this.priceSort.set('');
  }

  setRaritySelection(selection: DropdownSelection<unknown>) {
    this.rarity.set(selection.main as string);
  }


  setEquipmentTypeSelection(selection: DropdownSelection<unknown>) {
    this.equipmentType.set(selection.main as string);
  }

  setQualitySelection(selection: DropdownSelection<unknown>) {
    this.quality.set(selection.main as string);
  }

  selectListing(listing: MarketPlaceListing) {
    this.selectedListing.set(listing);
    this.selectedListingId = listing.id;
  }

  isOwnListing(listing: MarketPlaceListing): boolean {
    return listing.sellerId === this.characterState.currentCharacterId();
  }

  /* Handy trackBy for *ngFor */
  trackByListing = (_: number, l: MarketPlaceListing) => l.id;
}
