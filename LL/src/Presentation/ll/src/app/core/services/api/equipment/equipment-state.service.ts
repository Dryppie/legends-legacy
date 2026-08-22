import { Injectable, signal, computed, effect, untracked } from '@angular/core';
import { finalize, Observable, of, tap } from 'rxjs';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../shared/models/item';
import { EquipmentChangeResponse, EquipmentService } from './equipment.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { CharacterStateService } from '../character/character-state.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { VersionedMutationResult } from '../api.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';

@Injectable({ providedIn: 'root' })
export class EquipmentStateService {
  private readonly _equipmentSlots = signal<EquipmentSlot[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private resetVersion = 0;
  private loadEpoch = 0;

  readonly equipmentSlots = computed(() => this._equipmentSlots());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly isEmpty = computed(() =>
    this._equipmentSlots().every((slot) => !slot.equipmentInstance),
  );

  constructor(
    private readonly equipmentService: EquipmentService,
    private readonly inventoryState: InventoryStateService,
    private readonly eventBus: EventBusService,
    private readonly characterState: CharacterStateService,
    private readonly stateSync: StateSyncCoordinator,
    private readonly domainVersions: DomainVersionTracker,
  ) {
    this.stateSync.register('equipment', 'equipment', () =>
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
  }

  load(force = false): void {
    this.synchronize(force).subscribe({ error: () => undefined });
  }

  private synchronize(force = false): Observable<unknown> {
    if (!force && this._equipmentSlots().length) return of(undefined);
    this._loading.set(true);
    this._error.set(null);
    const requestVersion = this.resetVersion;
    const requestEpoch = ++this.loadEpoch;

    return this.equipmentService.getEquipment().pipe(
      tap({
        next: (equipmentSlots) => {
          if (
            requestVersion !== this.resetVersion ||
            requestEpoch !== this.loadEpoch
          ) {
            return;
          }
          this.applySlots(equipmentSlots);
        },
        error: (err) => {
          if (
            requestVersion !== this.resetVersion ||
            requestEpoch !== this.loadEpoch
          ) {
            return;
          }
          this._error.set(err.message ?? 'Unknown error');
        },
      }),
      finalize(() => {
        if (requestEpoch === this.loadEpoch) this._loading.set(false);
      }),
    );
  }

  reset(): void {
    this.resetVersion += 1;
    this.loadEpoch += 1;
    this.applySlots([]);
    this._loading.set(false);
    this._error.set(null);
  }

  setSlots(slots: EquipmentSlot[]): void {
    this.loadEpoch += 1;
    this.applySlots(slots);
  }

  private applySlots(slots: EquipmentSlot[]): void {
    this._equipmentSlots.set(slots);
    this.inventoryState.setEquippedItems(
      slots.flatMap((slot) =>
        slot.equipmentInstance ? [slot.equipmentInstance] : [],
      ),
    );
  }

  getSlot(slotType: EquipmentSlotType): EquipmentSlot | undefined {
    return this._equipmentSlots().find(
      (slot) => slot.equipmentSlotType === slotType,
    );
  }

  equip(
    equipmentInstance: EquipmentInstance,
    slotType: EquipmentSlotType,
  ): void {
    this._loading.set(true);
    this._error.set(null);

    this.equipmentService
      .equipEquipment(equipmentInstance, slotType)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => this.applyEquipmentChange(response),
        error: (err) =>
          this._error.set(
            err.errorMessage ?? err.message ?? 'Failed to equip item.',
          ),
      });
  }

  unequip(slotType: EquipmentSlotType): void {
    this._loading.set(true);
    this._error.set(null);

    this.equipmentService
      .unequipEquipment(slotType)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => this.applyEquipmentChange(response),
        error: (err) =>
          this._error.set(err.message ?? 'Failed to unequip item.'),
      });
  }

  private applyEquipmentChange(
    response: VersionedMutationResult<EquipmentChangeResponse>,
  ): void {
    if (
      this.domainVersions.isCurrent(
        'equipment',
        response.domainVersions['equipment'],
      )
    ) {
      this.setSlots(response.data.equipmentSlots);
    }
    this.inventoryState.applyVersionedInventory(response);
    this.characterState.markOverviewDirty();
  }
}
