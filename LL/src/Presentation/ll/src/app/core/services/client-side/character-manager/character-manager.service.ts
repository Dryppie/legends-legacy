import { effect, Injectable } from '@angular/core';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { InventoryDto } from '../../../../shared/models/Dtos/inventoryDto';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { BehaviorSubject } from 'rxjs';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { Equipment, EquipmentInstance } from '../../../../shared/models/item';
import { EventBusService } from '../event-bus/event-bus.service';
import { EquipmentType } from '../../../../shared/models/enums/equipmentType';

@Injectable({
  providedIn: 'root',
})
export class CharacterManagerService {
  private characterSubject = new BehaviorSubject<CharacterDto | null>(null);
  character$ = this.characterSubject.asObservable();

  private inventorySubject = new BehaviorSubject<InventoryDto | null>(null);
  inventory$ = this.inventorySubject.asObservable();

  private equipmentSubject = new BehaviorSubject<EquipmentSlot[]>([]);
  equipment$ = this.equipmentSubject.asObservable();

  constructor(private eventBus: EventBusService) {
    effect(() => {
      if (this.eventBus.logout()) {
        this.handleLogout();
      }
    });
  }

  setCharacter(character: CharacterDto | null) {
    this.characterSubject.next(character);
  }

  updateCharacter(partial: Partial<CharacterDto>) {
    const current = this.characterSubject.value;
    if (current) {
      this.characterSubject.next({ ...current, ...partial });
    }
  }

  getCharacter(): CharacterDto | null {
    return this.characterSubject.value;
  }

  // Inventory Methods
  setInventory(inventory: InventoryDto) {
    this.inventorySubject.next(inventory);
  }

  getInventory(): InventoryDto | null {
    return this.inventorySubject.value;
  }

  addItemToInventory(item: InventoryItem) {
    const current = this.inventorySubject.value;
    if (!current) return;
    const existingItem = current.inventoryItems.find((i) => i.id === item.id);
    if (existingItem) {
      existingItem.quantity += item.quantity;
    } else {
      current.inventoryItems.push(item);
    }
    this.inventorySubject.next({ ...current });
  }

  removeItemFromInventory(itemId: string, quantity: number = 1) {
    const current = this.inventorySubject.value;
    if (!current) return;
    const item = current.inventoryItems.find(
      (i) => i.itemInstance.id === itemId,
    );
    if (item) {
      item.quantity -= quantity;
      if (item.quantity <= 0) {
        current.inventoryItems = current.inventoryItems.filter(
          (i) => i.itemInstance.id !== itemId,
        );
      }
      this.inventorySubject.next({ ...current });
    }
  }

  // Equipment Methods
  setEquipment(equipmentSlots: EquipmentSlot[]) {
    this.equipmentSubject.next(equipmentSlots);
  }

  updateEquipment(equipmentInstance: EquipmentInstance) {
    const equipmentBase = equipmentInstance.itemBase as Equipment;
    const equipmentType = equipmentBase.equipmentType;

    const updated = this.equipmentSubject.value.map((slot) => ({ ...slot }));

    const getSlot = (type: EquipmentSlotType) =>
      updated.find((s) => s.equipmentSlotType === type);

    const unequip = (slot?: EquipmentSlot) => {
      if (slot) slot.equipmentInstance = undefined;
    };

    switch (equipmentType) {
      case EquipmentType.TwoHanded: {
        const main = getSlot(EquipmentSlotType.MainHand);
        const off = getSlot(EquipmentSlotType.OffHand);
        if (!main || !off) return;

        // Unequip both unless mainHand already holds a two-hander
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

        if (!main.equipmentInstance) {
          main.equipmentInstance = equipmentInstance;
        } else if (!off.equipmentInstance) {
          off.equipmentInstance = equipmentInstance;
        } else {
          const mainType = (main.equipmentInstance.itemBase as Equipment)
            .equipmentType;
          if (mainType === EquipmentType.TwoHanded) {
            unequip(off); // clear off-hand to break ghost state
          }

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

        if (mainType === EquipmentType.TwoHanded) {
          unequip(main);
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

    this.equipmentSubject.next(updated);
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
      case EquipmentType.Necklace:
        return EquipmentSlotType.Necklace;
      case EquipmentType.Ring:
        return EquipmentSlotType.Ring;
      default:
        throw new Error(`Unsupported equipment type: ${equipmentType}`);
    }
  }

  unequipEquipment(slotType: EquipmentSlotType) {
    const updated = this.equipmentSubject.value.map((es) => {
      if (es.equipmentSlotType === slotType) es.equipmentInstance = undefined;
      return es;
    });
    this.equipmentSubject.next(updated);
  }

  getEquipment(): EquipmentSlot[] {
    return this.equipmentSubject.value;
  }

  handleLogout() {
    this.characterSubject.next(null);
    this.inventorySubject.next(null);
    this.equipmentSubject.next([]);
  }
}
