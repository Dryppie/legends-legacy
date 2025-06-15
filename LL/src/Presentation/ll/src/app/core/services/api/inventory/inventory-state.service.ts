import { finalize } from 'rxjs';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { computed, effect, Injectable, signal } from '@angular/core';
import { InventoryService } from './inventory.service';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { GameSocketService } from '../../real-time/game-socket.service';

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
  private readonly _lastLoot = signal<InventoryItem[] | null>(null);

  constructor(
    private inventoryService: InventoryService,
    private readonly socket: GameSocketService,
  ) {
    this.load();

    effect(
      () => {
        const loot = this.socket.ofType('loot')();
        if (loot) {
          this._lastLoot.set(loot);
        }
      },
      { allowSignalWrites: true },
    );

    // React to new loot and mutate state once
    effect(
      () => {
        const loot = this._lastLoot();
        if (!loot) return;

        this.addOrIncrementMany(loot);
        this._lastLoot.set(null); // Clear to prevent retriggering
      },
      { allowSignalWrites: true },
    );
  }

  private handleLoot(loot: InventoryItem[]) {
    // ✅ Safe: Not inside an effect, can freely write to signals
    this.addOrIncrementMany(loot);
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

  setInventory(items: InventoryItem[]): void {
    this._items.set(items);
  }

  addOrIncrementMany(itemsToAdd: InventoryItem[]): void {
    for (const newItem of itemsToAdd) {
      this.addOrIncrement(newItem);
    }
  }

  addOrIncrement(item: InventoryItem): void {
    const items = this._items();
    const index = items.findIndex(
      (i) =>
        i.itemInstance.itemBase.stackable &&
        i.itemInstance.itemBase.id === item.itemInstance.itemBase.id,
    );

    if (index !== -1) {
      const updated = [...items];
      updated[index] = {
        ...updated[index],
        quantity: updated[index].quantity + item.quantity,
      };
      this._items.set(updated);
    } else {
      this._items.set([...items, item]);
    }
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
