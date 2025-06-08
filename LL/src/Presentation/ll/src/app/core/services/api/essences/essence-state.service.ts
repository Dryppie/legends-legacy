import { computed, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { EssencesService } from './essences.service';
import { Essence } from '../../../../shared/models/essence';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { EssenceSlot, SlotState } from '../../../../shared/models/essenceSlot';

@Injectable({ providedIn: 'root' })
export class EssenceStateService {
  /* ---------- writable signals ---------- */
  private readonly _essenceSlots = signal<EssenceSlot[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  /* ---------- public, read-only signals ---------- */
  readonly essenceSlots = computed(() => this._essenceSlots());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly isEmpty = computed(() => this._essenceSlots().length === 0);

  constructor(
    private essenceService: EssencesService,
    private inventoryState: InventoryStateService,
  ) {
    this.load();
  }

  load(): void {
    if (this._essenceSlots().length) return; // already cached
    this._loading.set(true);

    this.essenceService
      .getEquippedEssences()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (essences) => this._essenceSlots.set(essences),
        error: (err) =>
          this._error.set(err?.message ?? 'Failed to load essences'),
      });
  }

  add(essence: Essence): void {
    const backup = this._essenceSlots();
    const emptySlot = backup.find(
      (es) => es.occupiedEssence === null && es.slotState === SlotState.Active,
    );
    if (!emptySlot) return;

    emptySlot.occupiedEssence = essence;
    this._essenceSlots.set([...backup]);
    this.essenceService.equipEssence(essence.id).subscribe({
      error: () => this._essenceSlots.set(backup),
    });
  }

  remove(essenceId: string): void {
    const updated = this._essenceSlots().map((e) => {
      if (e.occupiedEssence?.id == essenceId) e.occupiedEssence = null;
      return e;
    });
    updated.sort((a, b) => {
      const aOccupied = a.occupiedEssence !== null ? 0 : 1;
      const bOccupied = b.occupiedEssence !== null ? 0 : 1;
      return aOccupied - bOccupied;
    });
    this._essenceSlots.set(updated);
    this.essenceService.deleteEquippedEssence(essenceId);
  }
}
