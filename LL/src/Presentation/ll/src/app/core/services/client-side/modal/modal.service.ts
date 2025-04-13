import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { Essence } from '../../../../shared/models/essence';
import { EquipmentInstance } from '../../../../shared/models/item';
import { EquipmentType } from '../../../../shared/models/Dtos/equipmentSlot';

@Injectable({
  providedIn: 'root',
})
export class ModalService {
  private essenceModalState = new BehaviorSubject<Essence | null>(null);
  private absorbEssenceModalState = new BehaviorSubject<Essence[] | null>(null);
  private removeEssenceModalState = new BehaviorSubject<Essence[] | null>(null);

  private inventoryEquipmentModalState =
    new BehaviorSubject<EquipmentInstance | null>(null);
  private overviewEquipmentModalState =
    new BehaviorSubject<EquipmentType | null>(null);

  private editCombatFiltersModalState = new BehaviorSubject<boolean>(false);

  essenceModalState$ = this.essenceModalState.asObservable();
  absorbEssenceModalState$ = this.absorbEssenceModalState.asObservable();
  removeEssenceModalState$ = this.removeEssenceModalState.asObservable();

  inventoryEquipmentModalState$ =
    this.inventoryEquipmentModalState.asObservable();

  overviewEquipmentModalState$ =
    this.overviewEquipmentModalState.asObservable();

  editCombatFiltersModalState$ =
    this.editCombatFiltersModalState.asObservable();

  toggleEssenceModal(essence: Essence | null = null): void {
    this.essenceModalState.next(essence);
  }

  toggleAbsorbEssenceModal(inventoryEssences: Essence[] | null = null): void {
    this.absorbEssenceModalState.next(inventoryEssences);
  }

  toggleRemoveEssenceModal(equippedEssences: Essence[] | null = null): void {
    this.removeEssenceModalState.next(equippedEssences);
  }

  toggleCombatFiltersModal(state: boolean = false): void {
    this.editCombatFiltersModalState.next(state);
  }

  toggleInventoryEquipItemModal(equipment: EquipmentInstance | null = null) {
    this.inventoryEquipmentModalState.next(equipment);
  }

  toggleOverviewEquipItemModal(equipment: EquipmentType | null = null) {
    this.overviewEquipmentModalState.next(equipment);
  }
}
