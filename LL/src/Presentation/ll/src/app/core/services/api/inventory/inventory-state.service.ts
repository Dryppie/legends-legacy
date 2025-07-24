import { finalize } from 'rxjs';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { computed, effect, Injectable, signal } from '@angular/core';
import { InventoryService } from './inventory.service';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { GameEventService } from '../../real-time/game-event.service';
import { EssenceItem } from '../../../../shared/models/item';

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
    private readonly eventService: GameEventService,
  ) {
    this.load();

    effect(
      () => {
        const loot = this.eventService.event.LootReceivedMsg();
        if (loot) {
          this._lastLoot.set(loot.payload);
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
  readonly materials = this.byType(ItemType.Resource);
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

  shatterEssences(essence: InventoryItem, shatterAmount: number) {
    this._loading.set(true);
    this.inventoryService
      .shatterEssence(essence, shatterAmount)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (gainedItem: InventoryItem) => {
          const items = [...this._items()];

          // Remove or reduce the shattered essence
          const essenceItem = items.find(
            (i) =>
              isEssenceItem(i.itemInstance.itemBase) &&
              isEssenceItem(essence.itemInstance.itemBase) &&
              i.itemInstance.itemBase.essence.id ===
                essence.itemInstance.itemBase.essence.id,
          );

          if (essenceItem) {
            essenceItem.quantity -= shatterAmount;
            if (essenceItem.quantity <= 0) {
              const index = items.indexOf(essenceItem);
              if (index !== -1) {
                items.splice(index, 1);
              }
            }
          }

          // Add or update the gained item (Soul Dust)
          const existing = items.find(
            (i) =>
              i.itemInstance.itemBase.id ===
              gainedItem.itemInstance.itemBase.id,
          );

          if (existing) {
            existing.quantity = gainedItem.quantity;
          } else {
            items.push(gainedItem);
          }

          this._items.set(items);
        },
        error: (err) => this._error.set(err.message ?? 'Unknown error'),
      });
  }

  scrapEquipment(equipmentIds: string[]) {
    this._loading.set(true);
    this.inventoryService
      .scrapEquipment(equipmentIds)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (gainedItem: InventoryItem) => {
          const items = this._items().filter(
            (i) => !equipmentIds.includes(i.itemInstance.id),
          );

          // Add or update the gained item (Soul Dust)
          const existing = items.find(
            (i) =>
              i.itemInstance.itemBase.id ===
              gainedItem.itemInstance.itemBase.id,
          );

          if (existing) {
            existing.quantity = gainedItem.quantity;
          } else {
            items.push(gainedItem);
          }

          this._items.set(items);
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

function isEssenceItem(item: any): item is EssenceItem {
  return item && 'essence' in item;
}
