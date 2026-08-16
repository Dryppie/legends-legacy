import { finalize } from 'rxjs';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { computed, effect, Injectable, signal, untracked } from '@angular/core';
import { InventoryService } from './inventory.service';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { GameEventService } from '../../real-time/game-event.service';
import { GameEventDeduper } from '../../real-time/game-event/game-event-consumer';
import { isGameRealtimeEnabled } from '../../real-time/game-realtime/game-realtime-feature';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';

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
  private readonly processedInventoryGrantIds = new Set<string>();
  private readonly processedInventoryGrantOrder: string[] = [];
  private readonly eventDeduper = new GameEventDeduper();
  private resetVersion = 0;
  private loadVersion = 0;

  constructor(
    private inventoryService: InventoryService,
    private readonly eventService: GameEventService,
    private readonly eventBus: EventBusService,
  ) {
    this.load();

    effect(
      () => {
        if (this.eventBus.logout()) {
          untracked(() => this.reset());
        }
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        if (isGameRealtimeEnabled()) return;

        const envelope = this.eventService.eventEnvelope.LootReceivedMsg();
        const loot = envelope?.payload;
        if (loot) {
          if (!this.eventDeduper.shouldProcess('loot-received', envelope)) {
            return;
          }

          const signature = this.getLootSignature(loot.payload);
          if (this.suppressedLootSignatures.delete(signature)) {
            return;
          }

          if (loot.grantId) {
            this.applyInventoryGrant(loot.grantId, loot.payload);
          } else {
            this._lastLoot.set(loot.payload);
          }
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

  /** How many items still carry the "new" marker. */
  readonly newItemCount = computed(
    () => this._items().filter((item) => item.isNew).length,
  );

  /* Ready-made selectors for common queries */
  readonly equipment = this.byType(ItemType.Equipment);
  readonly materials = this.byType(ItemType.Resource);
  readonly essences = this.byType(ItemType.Essence);

  load(force = false): void {
    if (!force && this._items().length) return; // already cached
    this._loading.set(true);
    this._error.set(null);
    const requestVersion = this.resetVersion;
    const loadVersion = ++this.loadVersion;

    this.inventoryService
      .getInventory()
      .pipe(
        finalize(() => {
          if (loadVersion === this.loadVersion) {
            this._loading.set(false);
          }
        }),
      )
      .subscribe({
        next: (dto) => {
          if (
            requestVersion !== this.resetVersion ||
            loadVersion !== this.loadVersion
          ) {
            return;
          }
          this._items.set(this.sortItems(dto.inventoryItems));
        },
        error: (err) => {
          if (
            requestVersion !== this.resetVersion ||
            loadVersion !== this.loadVersion
          ) {
            return;
          }
          this._error.set(err.message ?? 'Unknown error');
        },
      });
  }

  reset(): void {
    this.resetVersion += 1;
    this.loadVersion += 1;
    this._items.set([]);
    this._loading.set(false);
    this._error.set(null);
    this._lastLoot.set(null);
    this.suppressedLootSignatures.clear();
    this.processedInventoryGrantIds.clear();
    this.processedInventoryGrantOrder.length = 0;
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
        next: (response) =>
          this._items.set(this.sortItems(response.inventoryItems)),
        error: (err) => this._error.set(err.message ?? 'Unknown error'),
      });
  }

  setInventory(
    items: InventoryItem[],
    suppressNextLoot?: InventoryItem[],
  ): void {
    this._items.set(this.sortItems(items));

    if (suppressNextLoot?.length) {
      this.suppressNextLoot(suppressNextLoot);
    }
  }

  applyInventoryItemState(
    itemInstanceId: string,
    item: InventoryItem | null,
  ): void {
    if (!item) {
      this.removeItem(itemInstanceId);
      return;
    }

    const items = this._items();
    const index = items.findIndex(
      (current) => current.itemInstance.id === item.itemInstance.id,
    );
    if (index === -1) {
      this._items.set(this.sortItems([...items, item]));
      return;
    }

    const updated = [...items];
    updated[index] = item;
    this._items.set(this.sortItems(updated));
  }

  addOrIncrementMany(itemsToAdd: InventoryItem[]): void {
    if (!itemsToAdd.length) {
      return;
    }

    const updated = [...this._items()];

    for (const newItem of itemsToAdd) {
      const index = updated.findIndex(
        (i) =>
          i.itemInstance.itemBase.stackable &&
          i.itemInstance.itemBase.id === newItem.itemInstance.itemBase.id,
      );

      if (index !== -1) {
        updated[index] = {
          ...updated[index],
          quantity: updated[index].quantity + newItem.quantity,
        };
        continue;
      }

      updated.push(newItem);
    }

    this._items.set(this.sortItems(updated));
  }

  applyInventoryGrant(
    grantId: string | null | undefined,
    items: InventoryItem[],
  ): boolean {
    if (grantId && !this.markInventoryGrantProcessed(grantId)) {
      return false;
    }

    const hadInFlightSnapshot = this._loading();
    this.addOrIncrementMany(items);
    if (hadInFlightSnapshot) {
      this.load(true);
    }
    return true;
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

  /**
   * Clear an item's "new" marker and return the updated row.
   *
   * Optimistic: the local signal updates immediately and the write is fire-and-forget, because
   * `load()` short-circuits on a warm cache and a click that appears to do nothing is a worse
   * failure than a badge that reappears after a hard reload.
   */
  markSeen(itemInstanceId: string): InventoryItem | undefined {
    const items = this._items();
    const index = items.findIndex(
      (item) => item.itemInstance.id === itemInstanceId,
    );
    if (index === -1 || !items[index].isNew) return items[index];

    const updated = [...items];
    updated[index] = { ...updated[index], isNew: false };
    this._items.set(updated);

    this.inventoryService.markItemSeen(itemInstanceId).subscribe({
      error: () => undefined,
    });

    return updated[index];
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

  private markInventoryGrantProcessed(grantId: string): boolean {
    if (this.processedInventoryGrantIds.has(grantId)) {
      return false;
    }

    this.processedInventoryGrantIds.add(grantId);
    this.processedInventoryGrantOrder.push(grantId);
    while (this.processedInventoryGrantOrder.length > 500) {
      const expired = this.processedInventoryGrantOrder.shift();
      if (expired) this.processedInventoryGrantIds.delete(expired);
    }

    return true;
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
