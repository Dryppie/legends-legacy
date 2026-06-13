import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { ModalService } from '../../../core/services/client-side/modal/modal.service';
import { NgIf } from '@angular/common';
import { Essence } from '../../models/essence';
import { EssenceModalComponent } from './essence-modals/essence-modal/essence-modal.component';
import { CombatFiltersModalComponent } from './combat-modals/combat-filters-modal/combat-filters-modal.component';
import { EquipmentInstance } from '../../models/item';
import { InventoryEquipmentModalComponent } from './equipment-modals/equipment-modal/inventory-equipment-modal.component';
import { OverviewEquipmentModalComponent } from './equipment-modals/overview-equipment-modal/overview-equipment-modal.component';
import { EquipmentSlotType } from '../../models/Dtos/equipment-slots/equipmentSlot';

@Component({
  selector: 'app-modal-container',
  standalone: true,
  imports: [
    NgIf,
    EssenceModalComponent,
    CombatFiltersModalComponent,
    InventoryEquipmentModalComponent,
    OverviewEquipmentModalComponent,
  ],
  templateUrl: './modal-container.component.html',
})
export class ModalContainerComponent implements OnInit, OnDestroy {
  private subscriptions: Subscription[] = [];

  inventoryEquipment: EquipmentInstance | null = null;
  overviewEquipment: EquipmentSlotType | null = null;
  essence: Essence | null = null;
  filterCombat: boolean = false;

  constructor(private modalService: ModalService) {}

  ngOnInit() {
    this.subscriptions.push(
      this.modalService.inventoryEquipmentModalState$.subscribe(
        (data: EquipmentInstance | null) => (this.inventoryEquipment = data),
      ),
    );
    this.subscriptions.push(
      this.modalService.overviewEquipmentModalState$.subscribe(
        (data: EquipmentSlotType | null) => (this.overviewEquipment = data),
      ),
    );
    this.subscriptions.push(
      this.modalService.essenceModalState$.subscribe(
        (data: Essence | null) => (this.essence = data),
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
      !!this.inventoryEquipment ||
      !!this.overviewEquipment ||
      !!this.essence ||
      !!this.filterCombat
    );
  }

  onOverlayClick(event: MouseEvent) {
    // If you want to strictly check that the user clicked on the overlay itself:
    if (event.target === event.currentTarget) {
      // Closes whichever modal is open. If you have multiple modals open,
      // you'd close them accordingly.
      this.onInventoryEquipmentModalClose();
      this.onOverviewEquipmentModalClose();
      this.onEssenceModalClose();
      this.onEditCombatFiltersModalClose();
    }
  }

  onInventoryEquipmentModalClose() {
    this.modalService.toggleInventoryEquipItemModal();
  }
  onOverviewEquipmentModalClose() {
    this.modalService.toggleOverviewEquipItemModal();
  }

  onEssenceModalClose() {
    this.modalService.toggleEssenceModal();
  }

  onEditCombatFiltersModalClose() {
    this.modalService.toggleCombatFiltersModal();
  }
}
