import { Injectable, signal, computed } from '@angular/core';
import { finalize } from 'rxjs';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentType } from '../../../../shared/models/enums/equipmentType';
import { Equipment, EquipmentInstance } from '../../../../shared/models/item';
import { EquipmentService } from './equipment.service';
import { InventoryStateService } from '../inventory/inventory-state.service';

@Injectable({ providedIn: 'root' })
export class EquipmentStateService {
  /* ---------- writable signals ---------- */
  private readonly _equipmentSlots = signal<EquipmentSlot[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  /* ---------- public, read-only signals ---------- */
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

  load(): void {
    if (this._equipmentSlots().length) return; // already cached
    this._loading.set(true);
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

  equip(equipmentInstance: EquipmentInstance): void {
    const equipmentBase = equipmentInstance.itemBase as Equipment;
    const equipmentType = equipmentBase.equipmentType;

    this.equipmentService.equipEquipment(equipmentInstance);
    this.inventoryState.removeItem(equipmentInstance.id);

    const current = this._equipmentSlots();
    const updated = current.map((slot) => ({ ...slot }));

    const getSlot = (type: EquipmentSlotType) =>
      updated.find((s) => s.equipmentSlotType === type);

    const unequip = (slot?: EquipmentSlot) => {
      if (slot && slot.equipmentInstance) {
        this.inventoryState.add({
          id: crypto.randomUUID(),
          itemInstance: slot.equipmentInstance,
          quantity: 1,
        });
        slot.equipmentInstance = undefined;
      }
    };

    switch (equipmentType) {
      case EquipmentType.TwoHanded: {
        const main = getSlot(EquipmentSlotType.MainHand);
        const off = getSlot(EquipmentSlotType.OffHand);
        if (!main || !off) return;

        if (
          !main.equipmentInstance ||
          (main.equipmentInstance.itemBase as Equipment).equipmentType !==
            EquipmentType.TwoHanded
        ) {
          unequip(off);
        }

        unequip(main);
        main.equipmentInstance = equipmentInstance;
        off.equipmentInstance = equipmentInstance;
        break;
      }

      case EquipmentType.OneHanded: {
        const main = getSlot(EquipmentSlotType.MainHand);
        const off = getSlot(EquipmentSlotType.OffHand);
        if (!main || !off) return;

        const mainType = main.equipmentInstance
          ? (main.equipmentInstance.itemBase as Equipment).equipmentType
          : null;

        const offType = off.equipmentInstance
          ? (off.equipmentInstance.itemBase as Equipment).equipmentType
          : null;

        if (
          mainType === EquipmentType.TwoHanded ||
          offType === EquipmentType.TwoHanded
        ) {
          unequip(main);
          off.equipmentInstance = undefined;
        }

        if (!main.equipmentInstance) {
          main.equipmentInstance = equipmentInstance;
        } else if (!off.equipmentInstance) {
          off.equipmentInstance = equipmentInstance;
        } else {
          unequip(main);
          main.equipmentInstance = equipmentInstance;
        }
        break;
      }

      case EquipmentType.OffHand: {
        const main = getSlot(EquipmentSlotType.MainHand);
        const off = getSlot(EquipmentSlotType.OffHand);
        if (!main || !off) return;

        const mainType = main.equipmentInstance
          ? (main.equipmentInstance.itemBase as Equipment).equipmentType
          : null;

        const offType = off.equipmentInstance
          ? (off.equipmentInstance.itemBase as Equipment).equipmentType
          : null;

        if (
          mainType === EquipmentType.TwoHanded ||
          offType === EquipmentType.TwoHanded
        ) {
          main.equipmentInstance = undefined;
          unequip(off);
        }

        unequip(off);
        off.equipmentInstance = equipmentInstance;
        break;
      }

      default: {
        const slotType = this.getSlotTypeFromEquipmentType(equipmentType);
        const slot = getSlot(slotType);
        if (!slot) return;

        unequip(slot);
        slot.equipmentInstance = equipmentInstance;
        break;
      }
    }
    this._equipmentSlots.set(updated);
  }
  private getSlotTypeFromEquipmentType(
    equipmentType: EquipmentType,
  ): EquipmentSlotType {
    switch (equipmentType) {
      case EquipmentType.Head:
        return EquipmentSlotType.Head;
      case EquipmentType.Chest:
        return EquipmentSlotType.Chest;
      case EquipmentType.Legs:
        return EquipmentSlotType.Legs;
      case EquipmentType.Relic:
        return EquipmentSlotType.Relic;
      case EquipmentType.Relic:
        return EquipmentSlotType.Relic;
      case EquipmentType.Necklace:
        return EquipmentSlotType.Necklace;
      case EquipmentType.Ring:
        return EquipmentSlotType.Ring;
      default:
        throw new Error(`Unhandled equipment type: ${equipmentType}`);
    }
  }

  unequip(slotType: EquipmentSlotType): void {
    const current = this._equipmentSlots();
    const updated = current.map((slot) => ({ ...slot }));

    const getSlot = (type: EquipmentSlotType) =>
      updated.find((s) => s.equipmentSlotType === type);

    const main = getSlot(EquipmentSlotType.MainHand);
    const off = getSlot(EquipmentSlotType.OffHand);
    const target = getSlot(slotType);

    if (!target || !target.equipmentInstance) return;

    const instance = target.equipmentInstance;

    // Always add the unequipped item to inventory
    this.inventoryState.add({
      id: crypto.randomUUID(),
      itemInstance: instance,
      quantity: 1,
    });

    // If either main or off hand is TwoHanded, clear both slots
    const mainIsTwoHanded =
      main?.equipmentInstance?.itemBase.equipmentType ===
      EquipmentType.TwoHanded;
    const offIsTwoHanded =
      off?.equipmentInstance?.itemBase.equipmentType ===
      EquipmentType.TwoHanded;

    if (
      (slotType === EquipmentSlotType.MainHand ||
        slotType === EquipmentSlotType.OffHand) &&
      (mainIsTwoHanded || offIsTwoHanded)
    ) {
      if (main) main.equipmentInstance = undefined;
      if (off) off.equipmentInstance = undefined;
    } else {
      target.equipmentInstance = undefined;
    }

    this._equipmentSlots.set(updated);
    this.equipmentService.unequipEquipment(slotType);
  }
}
