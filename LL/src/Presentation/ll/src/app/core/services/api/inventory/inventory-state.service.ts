import { finalize } from 'rxjs';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { computed, Injectable, signal } from '@angular/core';
import { InventoryService } from './inventory.service';
import { ItemType } from '../../../../shared/models/enums/itemType';

@Injectable({ providedIn: 'root' })
export class InventoryStateService {
  /* ---------- writable signals ---------- */
  private readonly _items = signal<InventoryItem[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  /* ---------- public, read-only signals ---------- */
  readonly items = computed(() => this._items());
  readonly loading = computed(() => this._loading());
  readonly isEmpty = computed(() => this._items().length === 0);
  readonly error = computed(() => this._error());

  constructor(private inventoryService: InventoryService) {
    this.load();
  }

  /* Generic selector: reuse everywhere */
  byType = (type: ItemType) =>
    computed(
      () =>
        this._items().filter(
          (i) => i.itemInstance.itemBase.itemType === type,
        ) ?? [],
    );

  /* Ready-made selectors for common queries */
  readonly equipment = this.byType(ItemType.Equipment);
  readonly materials = this.byType(ItemType.Material);
  readonly essences = this.byType(ItemType.Essence);

  load(): void {
    if (this._items().length) return; // already cached
    this._loading.set(true);
    this.inventoryService
      .getInventory()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (dto) => {
          const sorted = dto.inventoryItems
            .slice() // defensive copy (optional)
            .sort((a, b) =>
              a.itemInstance.itemBase.itemType.localeCompare(
                b.itemInstance.itemBase.itemType,
              ),
            );

          this._items.set(sorted);
        },
        error: (err) => this._error.set(err.message ?? 'Unknown error'),
      });
  }

  add(item: InventoryItem): void {
    const backup = this._items(); // snapshot for rollback
    this._items.set([...backup, item]);
    //   this.http.post('/api/inventory', item).subscribe({
    //     error: () => this._items.set(backup), // rollback on failure
    //   });
  }

  decrementItem(itemInstanceId: string, qty: number): void {
    const updated = this._items()
      .map((item) => {
        if (item.itemInstance.id !== itemInstanceId) return item;

        if (item.itemInstance.itemBase.stackable) {
          const newQty = item.quantity - qty;
          if (newQty > 0) {
            return { ...item, quantity: newQty };
          }
        }
        // If new quantity is 0 or less, exclude it
        return null;
      })
      .filter((i): i is InventoryItem => i !== null);

    this._items.set(updated);
  }

  removeItem(itemInstanceId: string): void {
    const filtered = this._items().filter(
      (item) => item.itemInstance.id !== itemInstanceId,
    );
    this._items.set(filtered);
  }
}
