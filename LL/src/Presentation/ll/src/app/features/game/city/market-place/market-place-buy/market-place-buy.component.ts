import {
  NgClass,
  NgFor,
  NgIf,
  NgSwitch,
  NgSwitchCase,
  NgSwitchDefault,
} from '@angular/common';
import { Component, computed, effect, OnInit, signal } from '@angular/core';
import {
  FormControl,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { RegularButtonComponent } from '../../../../../shared/components/buttons/regular-button/regular-button.component';
import { MarketPlaceListing } from '../../../../../shared/models/Dtos/market-place/market-place-listing';
import { MarketplaceStateService } from '../../../../../core/services/api/market-place/market-place-state.service';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';
import { MarketPlaceListingItemComponent } from '../../../../../shared/components/market-place/market-place-listing-item/market-place-listing-item.component';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import { EquipmentInstance } from '../../../../../shared/models/item';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  debounceTime,
  distinctUntilChanged,
  map,
  startWith,
} from 'rxjs/operators';

@Component({
  selector: 'app-market-place-buy',
  standalone: true,
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
  ],
  templateUrl: './market-place-buy.component.html',
  styleUrl: './market-place-buy.component.css',
})
export class MarketPlaceBuyComponent implements OnInit {
  readonly allListings = signal<MarketPlaceListing[]>([]);
  selectedListingId: string = '';

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

  readonly itemType = signal<string>('');
  readonly rarity = signal<string>('');
  readonly priceSort = signal<'' | 'asc' | 'desc'>('');

  /** Dropdown data (could also come from enum/service) */
  readonly itemTypes = ['Equipment', 'Essence', 'Material'];
  readonly rarities = [
    'Common',
    'Uncommon',
    'Rare',
    'Epic',
    'Unique',
    'Legendary',
    'Legacy',
  ];

  /** Selected listing shown inside confirmation modal */
  readonly selectedListing = signal<MarketPlaceListing | null>(null);

  readonly filteredListings = computed(() => {
    let items = [...this.allListings()];

    const q = this.searchTerm();
    if (q) {
      items = items.filter((l) =>
        l.itemInstance.itemBase.name.toLowerCase().includes(q),
      );
    }

    /* 2️⃣ Category – via either dropdown or tab */
    const itemTypeFilter = this.itemType();
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

  constructor(private readonly marketplaceState: MarketplaceStateService) {
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

  buyoutListing(): void {
    const sel = this.selectedListing();
    if (!sel || this.qtyCtrl.invalid) return;
    const qty = this.qtyCtrl.value!;

    // 👇 Replace with your real service / API call
    this.marketplaceState.buyoutListing(sel.id, qty).subscribe({
      next: () => {
        // Optionally give the user feedback / toast here
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
    this.itemType.set('');
    this.rarity.set('');
    this.priceSort.set('asc');
  }

  selectListing(listing: MarketPlaceListing) {
    this.selectedListing.set(listing);
    this.selectedListingId = listing.id;
  }

  /* Handy trackBy for *ngFor */
  trackByListing = (_: number, l: MarketPlaceListing) => l.id;
}
