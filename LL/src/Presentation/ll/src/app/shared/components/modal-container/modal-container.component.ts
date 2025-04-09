import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { ModalService } from '../../../core/services/client-side/modal/modal.service';
import { NgIf } from '@angular/common';
import { Essence } from '../../models/essence';
import { EssenceModalComponent } from './essence-modals/essence-modal/essence-modal.component';
import { AbsorbEssenceModalComponent } from './essence-modals/absorb-essence-modal/absorb-essence-modal.component';
import { RemoveEssenceModalComponent } from './essence-modals/remove-essence-modal/remove-essence-modal.component';
import { CombatFiltersModalComponent } from './combat-modals/combat-filters-modal/combat-filters-modal.component';
import { EquipmentModalComponent } from './equipment-modals/equipment-modal/equipment-modal.component';
import { Equipment } from '../../models/item';

@Component({
  selector: 'app-modal-container',
  standalone: true,
  imports: [
    NgIf,
    EssenceModalComponent,
    AbsorbEssenceModalComponent,
    RemoveEssenceModalComponent,
    CombatFiltersModalComponent,
    EquipmentModalComponent,
  ],
  templateUrl: './modal-container.component.html',
  styleUrl: './modal-container.component.css',
})
export class ModalContainerComponent implements OnInit, OnDestroy {
  private subscriptions: Subscription[] = [];

  equipment: Equipment | null = null;
  essence: Essence | null = null;
  absorbEssence: Essence[] | null = null;
  removeEssence: Essence[] | null = null;
  filterCombat: boolean = false;

  constructor(private modalService: ModalService) {}

  ngOnInit() {
    this.subscriptions.push(
      this.modalService.equipmentModalState$.subscribe(
        (data: Equipment | null) => (this.equipment = data),
      ),
    );
    this.subscriptions.push(
      this.modalService.essenceModalState$.subscribe(
        (data: Essence | null) => (this.essence = data),
      ),
    );

    this.subscriptions.push(
      this.modalService.absorbEssenceModalState$.subscribe(
        (data: Essence[] | null) => (this.absorbEssence = data),
      ),
    );
    this.subscriptions.push(
      this.modalService.removeEssenceModalState$.subscribe(
        (data: Essence[] | null) => (this.removeEssence = data),
      ),
    );
    this.subscriptions.push(
      this.modalService.editCombatFiltersModalState$.subscribe((state) => {
        this.filterCombat = state;
      }),
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach((sub) => sub.unsubscribe());
  }

  // This can be as simple as checking if any modal is open
  // (here, we just have the essenceData for example).
  get isModalOpen(): boolean {
    return (
      !!this.equipment ||
      !!this.essence ||
      !!this.absorbEssence ||
      !!this.removeEssence ||
      !!this.filterCombat
    );
  }

  onOverlayClick(event: MouseEvent) {
    // If you want to strictly check that the user clicked on the overlay itself:
    if (event.target === event.currentTarget) {
      // Closes whichever modal is open. If you have multiple modals open,
      // you'd close them accordingly.
      this.onEquipmentModalClose();
      this.onEssenceModalClose();
      this.onAbsorbEssenceModalClose();
      this.onRemoveEssenceModalClose();
      this.onEditCombatFiltersModalClose();
    }
  }

  onEquipmentModalClose() {
    this.modalService.toggleEquipItemModal();
  }

  onEssenceModalClose() {
    this.modalService.toggleEssenceModal();
  }

  onAbsorbEssenceModalClose() {
    this.modalService.toggleAbsorbEssenceModal();
  }

  onRemoveEssenceModalClose() {
    this.modalService.toggleRemoveEssenceModal();
  }

  onEditCombatFiltersModalClose() {
    this.modalService.toggleCombatFiltersModal();
  }
}
