import { finalize } from 'rxjs';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { computed, effect, Injectable, signal } from '@angular/core';
import { InventoryService } from './inventory.service';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { GameEventService } from '../../real-time/game-event.service';

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
  private readonly suppressedLootSignatures = new Set<string>();

  constructor(
    private inventoryService: InventoryService,
    private readonly eventService: GameEventService,
  ) {
    this.load();

    effect(
      () => {
        const loot = this.eventService.event.LootReceivedMsg();
        if (loot) {
          const signature = this.getLootSignature(loot.payload);
          if (this.suppressedLootSignatures.delete(signature)) {
            return;
          }

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

    effect(
      () => {
        const reconnectCount = this.eventService.reconnectCount();
        if (reconnectCount > 0) {
          this.load(true);
        }
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

  load(force = false): void {
    if (!force && this._items().length) return; // already cached
    this._loading.set(true);
    this.inventoryService
      .getInventory()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (dto) => {
          this._items.set(this.sortItems(dto.inventoryItems));
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
        next: () => {
          const items = [...this._items()];

          const essenceItem = items.find(
            (i) => i.itemInstance.id === essence.itemInstance.id,
          );

          if (essenceItem) {
            essenceItem.quantity -= 1;
            if (essenceItem.quantity <= 0) {
              const index = items.indexOf(essenceItem);
              if (index !== -1) {
                items.splice(index, 1);
              }
            }
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

  setInventory(items: InventoryItem[], suppressNextLoot?: InventoryItem[]): void {
    this._items.set(this.sortItems(items));

    if (suppressNextLoot?.length) {
      this.suppressNextLoot(suppressNextLoot);
    }
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

  private suppressNextLoot(items: InventoryItem[]): void {
    const signature = this.getLootSignature(items);
    if (!signature) return;

    this.suppressedLootSignatures.add(signature);
    setTimeout(() => this.suppressedLootSignatures.delete(signature), 5000);
  }

  private getLootSignature(items: InventoryItem[]): string {
    return items
      .map(
        (item) =>
          `${item.itemInstance.id}:${item.itemInstance.itemBase.id}:${item.quantity}`,
      )
      .sort()
      .join('|');
  }

  private sortItems(items: InventoryItem[]): InventoryItem[] {
    return items
      .slice()
      .sort((a, b) =>
        a.itemInstance.itemBase.itemType.localeCompare(
          b.itemInstance.itemBase.itemType,
        ),
      );
  }
}

