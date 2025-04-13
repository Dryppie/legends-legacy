import { Injectable } from '@angular/core';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { InventoryDto } from '../../../../shared/models/Dtos/inventoryDto';
import {
  EquipmentSlot,
  EquipmentType,
} from '../../../../shared/models/Dtos/equipmentSlot';
import { BehaviorSubject } from 'rxjs';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { Equipment, EquipmentInstance } from '../../../../shared/models/item';

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

  constructor() {}

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
    const current = this.equipmentSubject.value.map((es) => {
      const equipmentBase = equipmentInstance.itemBase as Equipment;
      if (es.equipmentType === equipmentBase.equipmentType)
        es.equipmentInstance = equipmentInstance;
      return es;
    });
    this.equipmentSubject.next(current);
  }
  unequipEquipment(equipmentType: EquipmentType) {
    const updated = this.equipmentSubject.value.map((es) => {
      if (es.equipmentType === equipmentType) es.equipmentInstance = undefined;
      return es;
    });
    this.equipmentSubject.next(updated);
  }

  getEquipment(): EquipmentSlot[] {
    return this.equipmentSubject.value;
  }
}
