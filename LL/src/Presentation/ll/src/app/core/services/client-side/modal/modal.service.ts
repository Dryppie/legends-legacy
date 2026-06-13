import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { Essence } from '../../../../shared/models/essence';
import { EquipmentInstance } from '../../../../shared/models/item';
import { EquipmentSlotType } from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';

@Injectable({
  providedIn: 'root',
})
export class ModalService {
  private essenceModalState = new BehaviorSubject<Essence | null>(null);

  private inventoryEquipmentModalState =
    new BehaviorSubject<EquipmentInstance | null>(null);
  private overviewEquipmentModalState =
    new BehaviorSubject<EquipmentSlotType | null>(null);

  private editCombatFiltersModalState = new BehaviorSubject<boolean>(false);

  essenceModalState$ = this.essenceModalState.asObservable();

  inventoryEquipmentModalState$ =
    this.inventoryEquipmentModalState.asObservable();

  overviewEquipmentModalState$ =
    this.overviewEquipmentModalState.asObservable();

  editCombatFiltersModalState$ =
    this.editCombatFiltersModalState.asObservable();

  toggleEssenceModal(essence: Essence | null = null): void {
    this.essenceModalState.next(essence);
  }

  toggleCombatFiltersModal(state: boolean = false): void {
    this.editCombatFiltersModalState.next(state);
  }

  toggleInventoryEquipItemModal(equipment: EquipmentInstance | null = null) {
    this.inventoryEquipmentModalState.next(equipment);
  }

  toggleOverviewEquipItemModal(equipment: EquipmentSlotType | null = null) {
    this.overviewEquipmentModalState.next(equipment);
  }
}
