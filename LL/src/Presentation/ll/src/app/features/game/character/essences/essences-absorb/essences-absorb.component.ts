import { Component, computed, effect, signal } from '@angular/core';
import { EssenceStateService } from '../../../../../core/services/api/essences/essence-state.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { Essence } from '../../../../../shared/models/essence';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { EssenceItem } from '../../../../../shared/models/item';
import { NgIf } from '@angular/common';
import { RegularButtonComponent } from '../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { EssenceDetailsComponent } from '../../../../../shared/components/essences/essence-details/essence-details.component';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import { EssenceSlot } from '../../../../../shared/models/essenceSlot';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { FilterOption } from '../../../../../shared/components/list-filters/list-filter/filter-option';
import { SelectableListFilterComponent } from '../../../../../shared/components/list-filters/selectable-list-filter/selectable-list-filter.component';

@Component({
  selector: 'app-essences-absorb',
  standalone: true,
  imports: [
    NgIf,
    ReactiveFormsModule,
    FormsModule,
    RegularButtonComponent,
    EssenceDetailsComponent,
    SelectableListFilterComponent,
  ],
  templateUrl: './essences-absorb.component.html',
})
export class EssencesAbsorbComponent {
  showModal = false;
  shatterAmount: number = 0;

  readonly inventoryEssences = signal<InventoryItem[]>([]);
  readonly absorbedEssence = signal<EssenceSlot[]>([]);

  private readonly selectedItemInstanceId = signal<string | null>(null);
  readonly selectedEssence = computed<Essence | null>(() => {
    const id = this.selectedItemInstanceId();
    return id
      ? (
          (
            this.inventoryEssences().find((r) => r.itemInstance.id === id) ??
            null
          )?.itemInstance.itemBase as EssenceItem
        ).essence
      : null;
  });

  private readonly absorbedNames = computed(
    () =>
      new Set(
        this.absorbedEssence()
          .map((s) => s.occupiedEssence?.name)
          .filter(Boolean), // drop undefined
      ),
  );

  /** Filter predicates packaged for a generic filter component. */
  readonly essenceFilters: FilterOption<InventoryItem>[] = [
    {
      label: 'All',
      predicate: () => true,
    },
    {
      label: 'Already absorbed',
      predicate: (inv) =>
        this.absorbedNames().has(
          (inv.itemInstance.itemBase as EssenceItem).essence.name,
        ),
    },
    {
      label: 'Not absorbed',
      predicate: (inv) =>
        !this.absorbedNames().has(
          (inv.itemInstance.itemBase as EssenceItem).essence.name,
        ),
    },
  ];

  constructor(
    private essenceState: EssenceStateService,
    private inventoryState: InventoryStateService,
  ) {
    effect(
      () => {
        const items = this.inventoryState
          .items()
          .filter((i) => i.itemInstance.itemBase.itemType === ItemType.Essence);
        this.inventoryEssences.set(items);
        const essenceSlots = this.essenceState
          .essenceSlots()
          .filter((es) => es.occupiedEssence);
        this.absorbedEssence.set(essenceSlots);
      },
      { allowSignalWrites: true },
    );
  }

  selectEssence(inventoryItem: InventoryItem): void {
    this.selectedItemInstanceId.set(inventoryItem.itemInstance.id);
  }

  areSlotsAvailable(): boolean {
    return this.essenceState.essenceSlots().some((es) => !es.occupiedEssence);
  }

  isEssenceAbsorbed = computed(() => {
    return !!this.absorbedEssence().find(
      (i) => i.occupiedEssence?.id === this.selectedEssence()?.id,
    );
  });

  absorb() {
    const essence = this.selectedEssence();
    const inventoryItemId = this.selectedItemInstanceId();
    if (!essence || !inventoryItemId) return;
    this.selectedItemInstanceId.set(null);
    this.essenceState.add(essence);
    this.inventoryState.decrementItem(inventoryItemId, 1);
  }

  asEssence(inventoryItem: InventoryItem): Essence {
    return (inventoryItem.itemInstance.itemBase as EssenceItem).essence;
  }

  selectedEssenceQuantity() {
    return (
      this.inventoryEssences().find(
        (i) => this.asEssence(i).id === this.selectedEssence()?.id,
      )?.quantity ?? 1
    );
  }

  setShatterAmount(quantity: number) {
    this.shatterAmount = quantity;
  }

  disableConfirmShatterButton() {
    const essence = this.inventoryEssences().find(
      (i) => this.asEssence(i).id === this.selectedEssence()?.id,
    );
    return (
      !essence ||
      this.shatterAmount < 1 ||
      this.shatterAmount > essence.quantity
    );
  }

  confirmShatter() {
    const essence = this.inventoryEssences().find(
      (i) => this.asEssence(i).id === this.selectedEssence()?.id,
    );
    if (
      !essence ||
      this.shatterAmount < 1 ||
      this.shatterAmount > essence.quantity
    ) {
      // Optionally show a warning or validation error here
      return;
    }
    // Trigger your logic to actually perform the shatter
    this.shatterEssence(essence, this.shatterAmount);
    this.closeModal();
  }

  shatterEssence(essence: InventoryItem, shatterAmount: number) {
    this.inventoryState.shatterEssences(essence, shatterAmount);
  }

  openModal() {
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
    this.shatterAmount = 0;
  }
}
