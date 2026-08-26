import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';
import { Subscription } from 'rxjs';
import { ModalService } from '../../../core/services/client-side/modal/modal.service';
import { NgClass, NgIf } from '@angular/common';
import { Essence } from '../../models/essence';
import { EssenceModalComponent } from './essence-modals/essence-modal/essence-modal.component';
import { CombatFiltersModalComponent } from './combat-modals/combat-filters-modal/combat-filters-modal.component';
import { EquipmentInstance } from '../../models/item';
import { InventoryEquipmentModalComponent } from './equipment-modals/equipment-modal/inventory-equipment-modal.component';
import { OverviewEquipmentModalComponent } from './equipment-modals/overview-equipment-modal/overview-equipment-modal.component';
import { EquipmentSlotType } from '../../models/Dtos/equipment-slots/equipmentSlot';
import { InventoryItem } from '../../models/inventoryItem';
import { InventoryItemModalComponent } from './item-modals/inventory-item-modal/inventory-item-modal.component';

@Component({
  selector: 'app-modal-container',
  imports: [
    NgIf,
    NgClass,
    EssenceModalComponent,
    CombatFiltersModalComponent,
    InventoryItemModalComponent,
    InventoryEquipmentModalComponent,
    OverviewEquipmentModalComponent,
    A11yModule,
  ],
  templateUrl: './modal-container.component.html',
})
export class ModalContainerComponent implements OnInit, OnDestroy {
  private subscriptions: Subscription[] = [];
  private lastFocusedElement: HTMLElement | null = null;

  inventoryEquipment: EquipmentInstance | null = null;
  inventoryItem: InventoryItem | null = null;
  overviewEquipment: EquipmentSlotType | null = null;
  essence: Essence | null = null;
  filterCombat: boolean = false;

  constructor(private modalService: ModalService) {}

  ngOnInit() {
    this.subscriptions.push(
      this.modalService.inventoryItemModalState$.subscribe(
        (data: InventoryItem | null) =>
          this.updateModalState(() => (this.inventoryItem = data)),
      ),
    );
    this.subscriptions.push(
      this.modalService.inventoryEquipmentModalState$.subscribe(
        (data: EquipmentInstance | null) =>
          this.updateModalState(() => (this.inventoryEquipment = data)),
      ),
    );
    this.subscriptions.push(
      this.modalService.overviewEquipmentModalState$.subscribe(
        (data: EquipmentSlotType | null) =>
          this.updateModalState(() => (this.overviewEquipment = data)),
      ),
    );
    this.subscriptions.push(
      this.modalService.essenceModalState$.subscribe((data: Essence | null) =>
        this.updateModalState(() => (this.essence = data)),
      ),
    );

    this.subscriptions.push(
      this.modalService.editCombatFiltersModalState$.subscribe((state) => {
        this.updateModalState(() => (this.filterCombat = state));
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
      !!this.inventoryItem ||
      !!this.overviewEquipment ||
      !!this.essence ||
      !!this.filterCombat
    );
  }

  get modalLabel(): string {
    if (this.inventoryEquipment) return 'Equipment details';
    if (this.inventoryItem) return 'Item details';
    if (this.overviewEquipment) return 'Equipped item details';
    if (this.essence) return 'Essence details';
    return 'Combat filters';
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscape(event: KeyboardEvent): void {
    if (!this.isModalOpen) return;
    event.preventDefault();
    event.stopPropagation();
    this.closeCurrentModal();
  }

  onOverlayClick(event: MouseEvent) {
    // If you want to strictly check that the user clicked on the overlay itself:
    if (event.target === event.currentTarget) {
      this.closeCurrentModal();
    }
  }

  onInventoryEquipmentModalClose() {
    this.modalService.toggleInventoryEquipItemModal();
  }

  onInventoryItemModalClose() {
    this.modalService.toggleInventoryItemModal();
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

  private closeCurrentModal(): void {
    if (this.inventoryEquipment) this.onInventoryEquipmentModalClose();
    else if (this.inventoryItem) this.onInventoryItemModalClose();
    else if (this.overviewEquipment) this.onOverviewEquipmentModalClose();
    else if (this.essence) this.onEssenceModalClose();
    else if (this.filterCombat) this.onEditCombatFiltersModalClose();
  }

  private updateModalState(update: () => void): void {
    const wasOpen = this.isModalOpen;
    if (!wasOpen) {
      this.lastFocusedElement =
        document.activeElement instanceof HTMLElement
          ? document.activeElement
          : null;
    }

    update();

    if (wasOpen && !this.isModalOpen) {
      const target = this.lastFocusedElement;
      this.lastFocusedElement = null;
      queueMicrotask(() => target?.focus());
    }
  }
}
