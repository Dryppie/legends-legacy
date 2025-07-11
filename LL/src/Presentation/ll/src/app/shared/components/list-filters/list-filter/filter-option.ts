export interface FilterOption<T> {
  label: string;
  /** Return true if the item should be shown when this filter is active. */
  predicate: (item: T) => boolean;
}
