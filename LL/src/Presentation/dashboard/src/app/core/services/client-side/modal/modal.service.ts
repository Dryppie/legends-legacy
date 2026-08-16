import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { Essence } from '../../../../shared/models/essence';
import { Equipment } from '../../../../shared/models/item';

@Injectable({ providedIn: 'root' })
export class ModalService {
  private readonly essenceModalState = new BehaviorSubject<Essence | null>(null);
  private readonly absorbEssenceModalState =
    new BehaviorSubject<Essence[] | null>(null);
  private readonly removeEssenceModalState =
    new BehaviorSubject<Essence[] | null>(null);
  private readonly equipmentModalState =
    new BehaviorSubject<Equipment | null>(null);
  private readonly editCombatFiltersModalState =
    new BehaviorSubject<boolean>(false);

  readonly essenceModalState$ = this.essenceModalState.asObservable();
  readonly absorbEssenceModalState$ =
    this.absorbEssenceModalState.asObservable();
  readonly removeEssenceModalState$ =
    this.removeEssenceModalState.asObservable();
  readonly equipmentModalState$ = this.equipmentModalState.asObservable();
  readonly editCombatFiltersModalState$ =
    this.editCombatFiltersModalState.asObservable();

  toggleEssenceModal(essence: Essence | null = null): void {
    this.essenceModalState.next(essence);
  }

  toggleAbsorbEssenceModal(essences: Essence[] | null = null): void {
    this.absorbEssenceModalState.next(essences);
  }

  toggleRemoveEssenceModal(essences: Essence[] | null = null): void {
    this.removeEssenceModalState.next(essences);
  }

  toggleCombatFiltersModal(state = false): void {
    this.editCombatFiltersModalState.next(state);
  }

  toggleEquipItemModal(equipment: Equipment | null = null): void {
    this.equipmentModalState.next(equipment);
  }
}
