import {
  catchError,
  finalize,
  map,
  Observable,
  of,
  tap,
  throwError,
} from 'rxjs';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { computed, effect, Injectable, signal, untracked } from '@angular/core';
import { InventoryService } from './inventory.service';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { VersionedMutationResult } from '../api.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { BusinessGrantDeduper } from '../../real-time/game-realtime/realtime-deduplication';

@Injectable({ providedIn: 'root' })
export class InventoryStateService {
  /* ---------- writable signals ---------- */
  private readonly _items = signal<InventoryItem[]>([]);
  private readonly _equippedItemFavoriteState = signal<
    ReadonlyMap<string, boolean>
  >(new Map());
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  /* ---------- public, read-only signals ---------- */
  readonly items = computed(() => this._items());
  readonly loading = computed(() => this._loading());
  readonly isEmpty = computed(() => this._items().length === 0);
  readonly error = computed(() => this._error());
  private readonly favoriteItemInstanceIds = computed(() => {
    const favorites = new Set(
      this._items()
        .filter((item) => item.isFavorite)
        .map((item) => item.itemInstance.id),
    );
    for (const [
      itemInstanceId,
      isFavorite,
    ] of this._equippedItemFavoriteState()) {
      if (isFavorite) favorites.add(itemInstanceId);
    }
    return favorites;
  });
  private readonly _lastLoot = signal<InventoryItem[] | null>(null);
  private readonly inventoryGrantDeduper = new BusinessGrantDeduper();
  private resetVersion = 0;
  private loadVersion = 0;

  constructor(
    private inventoryService: InventoryService,
    private readonly eventBus: EventBusService,
    private readonly stateSync: StateSyncCoordinator,
    private readonly domainVersions: DomainVersionTracker,
  ) {
    this.stateSync.register('inventory', 'inventory', () =>
      this.synchronize(true),
    );
    this.load();

    effect(
      () => {
        if (this.eventBus.logout()) {
          untracked(() => this.reset());
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

  /** How many items still carry the "new" marker. */
  readonly newItemCount = computed(
    () => this._items().filter((item) => item.isNew).length,
  );

  /* Ready-made selectors for common queries */
  readonly equipment = this.byType(ItemType.Equipment);
  readonly materials = this.byType(ItemType.Resource);
  readonly essences = this.byType(ItemType.Essence);

  load(force = false): void {
    this.synchronize(force).subscribe({ error: () => undefined });
  }

  private synchronize(force = false): Observable<unknown> {
    if (!force && this._items().length) return of(undefined); // already cached
    this._loading.set(true);
    this._error.set(null);
    const requestVersion = this.resetVersion;
    const loadVersion = ++this.loadVersion;

    return this.inventoryService.getInventory().pipe(
      tap({
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
      }),
      finalize(() => {
        if (loadVersion === this.loadVersion) {
          this._loading.set(false);
        }
      }),
    );
  }

  reset(): void {
    this.resetVersion += 1;
    this.loadVersion += 1;
    this._items.set([]);
    this._equippedItemFavoriteState.set(new Map());
    this._loading.set(false);
    this._error.set(null);
    this._lastLoot.set(null);
    this.inventoryGrantDeduper.clear();
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

          this.setInventory(items);
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
        next: (response) => this.applyVersionedInventory(response),
        error: (err) => this._error.set(err.message ?? 'Unknown error'),
      });
  }

  applyVersionedInventory<T extends { inventoryItems: InventoryItem[] }>(
    result: VersionedMutationResult<T>,
    grantId?: string | null,
  ): boolean {
    if (
      !this.domainVersions.isCurrent(
        'inventory',
        result.domainVersions['inventory'],
      )
    ) {
      return false;
    }

    if (!this.inventoryGrantDeduper.shouldApply(grantId)) {
      return false;
    }

    this.setInventory(result.data.inventoryItems);
    return true;
  }

  applyVersionedInventoryDelta<T>(
    result: VersionedMutationResult<T>,
    apply: (data: T) => void,
  ): boolean {
    if (
      !this.domainVersions.isCurrent(
        'inventory',
        result.domainVersions['inventory'],
      )
    ) {
      return false;
    }

    this.invalidateInFlightLoad();
    apply(result.data);
    return true;
  }

  setInventory(items: InventoryItem[]): void {
    this.invalidateInFlightLoad();
    this._items.set(this.sortItems(items));
  }

  setEquippedItems(
    items: ReadonlyArray<{ id: string; isFavorite?: boolean }>,
  ): void {
    this._equippedItemFavoriteState.set(
      new Map(items.map((item) => [item.id, !!item.isFavorite])),
    );
  }

  applyInventoryItemState(
    itemInstanceId: string,
    item: InventoryItem | null,
  ): void {
    if (!item) {
      this.removeItem(itemInstanceId);
      return;
    }

    this.invalidateInFlightLoad();
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

    this.invalidateInFlightLoad();
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
    if (!this.inventoryGrantDeduper.shouldApply(grantId)) {
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
    this.invalidateInFlightLoad();
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
    this.invalidateInFlightLoad();
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

    this.invalidateInFlightLoad();
    const updated = [...items];
    updated[index] = { ...updated[index], isNew: false };
    this._items.set(updated);

    this.inventoryService.markItemSeen(itemInstanceId).subscribe({
      next: (response) => this.applyVersionedInventory(response),
      error: () => undefined,
    });

    return updated[index];
  }

  /**
   * Update the favorite marker immediately, then persist it. A failed request rolls back
   * only if no newer favorite change has replaced the optimistic value.
   */
  setFavorite(
    itemInstanceId: string,
    isFavorite: boolean,
  ): Observable<InventoryItem | undefined> {
    const items = this._items();
    const index = items.findIndex(
      (item) => item.itemInstance.id === itemInstanceId,
    );
    const equippedFavorites = this._equippedItemFavoriteState();
    const isEquipped = equippedFavorites.has(itemInstanceId);
    if (index === -1 && !isEquipped) {
      return throwError(
        () => new Error('The item is no longer owned by this character.'),
      );
    }

    const previous = index === -1 ? undefined : items[index];
    const previousEquippedFavorite = equippedFavorites.get(itemInstanceId);
    this.invalidateInFlightLoad();
    if (previous) {
      const optimistic = { ...previous, isFavorite };
      const updated = [...items];
      updated[index] = optimistic;
      this._items.set(updated);
    } else {
      const updated = new Map(equippedFavorites);
      updated.set(itemInstanceId, isFavorite);
      this._equippedItemFavoriteState.set(updated);
    }

    return this.inventoryService
      .setItemFavorite(itemInstanceId, isFavorite)
      .pipe(
        map((response) => {
          const applied = this.applyVersionedInventory(response);
          if (applied && isEquipped) {
            const current = new Map(this._equippedItemFavoriteState());
            current.set(itemInstanceId, response.data.isFavorite);
            this._equippedItemFavoriteState.set(current);
          }
          return this._items().find(
            (item) => item.itemInstance.id === itemInstanceId,
          );
        }),
        catchError((error) => {
          const currentItems = this._items();
          const currentIndex = currentItems.findIndex(
            (item) => item.itemInstance.id === itemInstanceId,
          );
          if (
            previous &&
            currentIndex !== -1 &&
            currentItems[currentIndex].isFavorite === isFavorite
          ) {
            const rolledBack = [...currentItems];
            rolledBack[currentIndex] = previous;
            this._items.set(rolledBack);
          } else if (!previous) {
            const currentEquippedFavorites = this._equippedItemFavoriteState();
            if (currentEquippedFavorites.get(itemInstanceId) === isFavorite) {
              const rolledBack = new Map(currentEquippedFavorites);
              rolledBack.set(itemInstanceId, !!previousEquippedFavorite);
              this._equippedItemFavoriteState.set(rolledBack);
            }
          }

          return throwError(() => error);
        }),
      );
  }

  isFavorite(itemInstanceId: string): boolean {
    return this.favoriteItemInstanceIds().has(itemInstanceId);
  }

  removeItem(itemInstanceId: string): void {
    this.invalidateInFlightLoad();
    const filtered = this._items().filter(
      (item) => item.itemInstance.id !== itemInstanceId,
    );
    this._items.set(filtered);
  }

  private invalidateInFlightLoad(): void {
    // A local delta or mutation response is newer than any inventory GET
    // already in flight, so that GET must not replace the patched state.
    this.loadVersion += 1;
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
