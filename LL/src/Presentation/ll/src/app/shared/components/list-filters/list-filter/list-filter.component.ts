import { NgClass, NgFor, NgIf, NgTemplateOutlet } from '@angular/common';
import {
  Component,
  computed,
  ContentChild,
  effect,
  EventEmitter,
  input,
  Output,
  signal,
  TemplateRef,
} from '@angular/core';
import { FilterOption } from './filter-option';

@Component({
  selector: 'app-list-filter',
  standalone: true,
  imports: [NgIf, NgFor, NgClass, NgTemplateOutlet],
  templateUrl: './list-filter.component.html',
})
export class ListFilterComponent<T> {
  /** Raw data to filter. */
  items = input.required<T[]>();
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

  constructor() {
    effect(
      () => {
        const filters = this.filterOptions();
        // Only set if not already active and there's at least one option
        if (!this.active() && filters.length > 0) {
          this.active.set(filters[0]);
        }
      },
      { allowSignalWrites: true },
    );
  }

  /** Switch active filter. */
  setActive(opt: FilterOption<T>) {
    this.active.set(opt);
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
