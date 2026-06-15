import { Injectable, signal, computed } from '@angular/core';
import { finalize } from 'rxjs';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../shared/models/item';
import {
  EquipmentChangeResponse,
  EquipmentService,
} from './equipment.service';
import { InventoryStateService } from '../inventory/inventory-state.service';

@Injectable({ providedIn: 'root' })
export class EquipmentStateService {
  private readonly _equipmentSlots = signal<EquipmentSlot[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly equipmentSlots = computed(() => this._equipmentSlots());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly isEmpty = computed(() =>
    this._equipmentSlots().every((slot) => !slot.equipmentInstance),
  );

  constructor(
    private readonly equipmentService: EquipmentService,
    private readonly inventoryState: InventoryStateService,
  ) {
    this.load();
  }

  load(force = false): void {
    if (!force && this._equipmentSlots().length) return;
    this._loading.set(true);
    this._error.set(null);

    this.equipmentService
      .getEquipment()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (equipmentSlots) => this._equipmentSlots.set(equipmentSlots),
        error: (err) => this._error.set(err.message ?? 'Unknown error'),
      });
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
        error: (err) =>
          this._error.set(err.message ?? 'Failed to equip item.'),
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
  }
}
