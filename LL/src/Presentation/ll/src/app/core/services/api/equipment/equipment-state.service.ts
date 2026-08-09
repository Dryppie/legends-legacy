import { Injectable, signal, computed, effect, untracked } from '@angular/core';
import { finalize } from 'rxjs';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../shared/models/item';
import { EquipmentChangeResponse, EquipmentService } from './equipment.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { CharacterStateService } from '../character/character-state.service';
import { QuestStateService } from '../quest/quest-state.service';

@Injectable({ providedIn: 'root' })
export class EquipmentStateService {
  private readonly _equipmentSlots = signal<EquipmentSlot[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private resetVersion = 0;

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
    private readonly questState: QuestStateService,
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
  }

  load(force = false): void {
    if (!force && this._equipmentSlots().length) return;
    this._loading.set(true);
    this._error.set(null);
    const requestVersion = this.resetVersion;

    this.equipmentService
      .getEquipment()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (equipmentSlots) => {
          if (requestVersion !== this.resetVersion) return;
          this._equipmentSlots.set(equipmentSlots);
        },
        error: (err) => {
          if (requestVersion !== this.resetVersion) return;
          this._error.set(err.message ?? 'Unknown error');
        },
      });
  }

  reset(): void {
    this.resetVersion += 1;
    this._equipmentSlots.set([]);
    this._loading.set(false);
    this._error.set(null);
  }

  setSlots(slots: EquipmentSlot[]): void {
    this._equipmentSlots.set(slots);
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
        error: (err) => this._error.set(err.message ?? 'Failed to equip item.'),
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

  private applyEquipmentChange(response: EquipmentChangeResponse): void {
    this._equipmentSlots.set(response.equipmentSlots);
    this.inventoryState.setInventory(response.inventoryItems);
    this.characterState.markOverviewDirty();
    this.questState.refreshAfterOutboxProgress();
  }
}
