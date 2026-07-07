import {
  Component,
  computed,
  ContentChild,
  effect,
  EventEmitter,
  Input,
  input,
  Output,
  signal,
  TemplateRef,
} from '@angular/core';
import { FilterOption } from '../list-filter/filter-option';
import { NgClass, NgFor, NgIf, NgTemplateOutlet } from '@angular/common';
import { LocalStorageService } from '../../../../core/services/client-side/local-storage/local-storage.service';

@Component({
  selector: 'app-selectable-list-filter',
  standalone: true,
  imports: [NgIf, NgFor, NgClass, NgTemplateOutlet],
  templateUrl: './selectable-list-filter.component.html',
})
export class SelectableListFilterComponent<T> {
  @Input() storageKey?: string;
  @Input() tourItemId: string | null = null;

  /** Raw data to filter. */
  items = input.required<T[]>();
  selectedItem = input<T | null>(null);
  selected: T | null = null;
  /** All available filters (first one will be the default). */
  filterOptions = input.required<FilterOption<T>[]>();

  /** Emits the *currently* selected item (optional). */
  @Output() select = new EventEmitter<T>();

  /* Grab the consumer’s row template */
  @ContentChild(TemplateRef, { static: false })
  itemTpl!: TemplateRef<unknown>;

  /** Which filter is active? */
  active = signal<FilterOption<T> | null>(null);

  constructor(private readonly storage: LocalStorageService) {
    effect(() => {
      this.selected = this.selectedItem();
    });

    effect(
      () => {
        const filters = this.filterOptions();

        if (filters.length === 0) return;

        const key = this.storageKey;
        if (key) {
          const saved = this.storage.get<string>(key);
          const match = filters.find((f) => f.label === saved);
          if (match) {
            this.active.set(match);
            return;
          }
        }

        // Fallback: use the first one
        if (!this.active()) {
          this.active.set(filters[0]);
        }
      },
      { allowSignalWrites: true },
    );
  }

  /** Switch active filter. */
  setActive(opt: FilterOption<T>) {
    this.active.set(opt);
    const current = this.active();
    const key = this.storageKey;
    if (current && key) {
      this.storage.set(key, current.label);
    }
  }

  /** Currently visible items. */
  filtered = computed(() => {
    const list = this.items(); // now reactive ✅
    const opt = this.active() ?? this.filterOptions()[0];
    return list.filter(opt.predicate);
  });

  /** Handle item click. */
  onPick(item: T) {
    this.select.emit(item);
    this.selected = item;
  }
}
