import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { HostListener } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';
import { EssenceStateService } from '../../../../../core/services/api/essences/essence-state.service';
import { RegularButtonComponent } from '../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { EssenceDetailsComponent } from '../../../../../shared/components/essences/essence-details/essence-details.component';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { EssenceItem } from '../../../../../shared/models/item';

type AbsorbFilter = 'all' | 'absorbed' | 'not-absorbed';
type AbsorbSort = 'name' | 'quantity' | 'status';

@Component({
  selector: 'app-essences-absorb',
  imports: [
    NgClass,
    NgFor,
    NgIf,
    RegularButtonComponent,
    EssenceDetailsComponent,
    A11yModule,
  ],
  templateUrl: './essences-absorb.component.html',
  styleUrl: './essences-absorb.component.scss',
})
export class EssencesAbsorbComponent {
  private modalTrigger: HTMLElement | null = null;
  showModal = false;
  shatterMode: 'single' | 'bulk' = 'single';

  readonly activeFilter = signal<AbsorbFilter>('all');
  readonly activeSort = signal<AbsorbSort>('name');
  readonly selectedShatterIds = signal<ReadonlySet<string>>(new Set());
  readonly shattering = signal(false);
  readonly singleShatterQuantity = signal(1);
  readonly mobileSelectionMode = signal(false);
  readonly mobileDetailOpen = signal(false);

  readonly visibleEssences = computed(() => {
    const filter = this.activeFilter();
    const sort = this.activeSort();
    const items = this.essenceState.inventoryEssences().filter((item) => {
      const isAbsorbed = this.essenceState.isInventoryEssenceAbsorbed(item);
      if (filter === 'absorbed') return isAbsorbed;
      if (filter === 'not-absorbed') return !isAbsorbed;
      return true;
    });

    return [...items].sort((left, right) => {
      if (sort === 'quantity') {
        return right.quantity - left.quantity;
      }

      if (sort === 'status') {
        const statusDifference =
          Number(this.essenceState.isInventoryEssenceAbsorbed(right)) -
          Number(this.essenceState.isInventoryEssenceAbsorbed(left));
        if (statusDifference !== 0) return statusDifference;
      }

      return this.essenceState
        .asEssence(left)
        .name.localeCompare(this.essenceState.asEssence(right).name);
    });
  });

  readonly selectedShatterItems = computed(() =>
    this.essenceState
      .inventoryEssences()
      .filter(
        (item) =>
          this.selectedShatterIds().has(item.itemInstance.id) &&
          this.spareCopies(item) > 0,
      ),
  );

  readonly selectedShatterCount = computed(() =>
    this.selectedShatterItems().reduce(
      (total, item) => total + this.spareCopies(item),
      0,
    ),
  );

  readonly selectedShatterDust = computed(() =>
    this.selectedShatterItems().reduce(
      (total, item) =>
        total +
        this.spareCopies(item) *
          Math.max(
            1,
            (item.itemInstance.itemBase as EssenceItem).dismantleDustAmount,
          ),
      0,
    ),
  );

  readonly absorbedDuplicateCandidates = computed(() =>
    this.essenceState
      .inventoryEssences()
      .filter(
        (item) =>
          this.essenceState.isInventoryEssenceAbsorbed(item) &&
          this.spareCopies(item) > 0,
      ),
  );

  readonly allAbsorbedDuplicatesSelected = computed(() => {
    const candidates = this.absorbedDuplicateCandidates();
    return (
      candidates.length > 0 &&
      candidates.every((item) =>
        this.selectedShatterIds().has(item.itemInstance.id),
      )
    );
  });

  readonly someAbsorbedDuplicatesSelected = computed(() => {
    const selectedIds = this.selectedShatterIds();
    const selectedCount = this.absorbedDuplicateCandidates().filter((item) =>
      selectedIds.has(item.itemInstance.id),
    ).length;
    return (
      selectedCount > 0 &&
      selectedCount < this.absorbedDuplicateCandidates().length
    );
  });

  constructor(public readonly essenceState: EssenceStateService) {}

  selectEssence(inventoryItem: InventoryItem): void {
    this.singleShatterQuantity.set(1);
    this.essenceState.selectInventoryEssence(inventoryItem);
    this.mobileDetailOpen.set(true);
  }

  closeMobileDetail(): void {
    this.mobileDetailOpen.set(false);
  }

  toggleMobileSelectionMode(): void {
    if (this.mobileSelectionMode()) {
      this.clearShatterSelection();
      this.mobileSelectionMode.set(false);
      return;
    }

    this.closeMobileDetail();
    this.mobileSelectionMode.set(true);
  }

  setFilter(filter: AbsorbFilter): void {
    this.activeFilter.set(filter);
  }

  setSort(event: Event): void {
    this.activeSort.set(
      (event.target as HTMLSelectElement).value as AbsorbSort,
    );
  }

  isSelected(inventoryItem: InventoryItem): boolean {
    return (
      this.essenceState.selectedInventoryItem()?.itemInstance.id ===
      inventoryItem.itemInstance.id
    );
  }

  spareCopies(inventoryItem: InventoryItem): number {
    return Math.max(
      0,
      inventoryItem.quantity -
        (this.essenceState.isInventoryEssenceAbsorbed(inventoryItem) ? 0 : 1),
    );
  }

  selectedSpareCopies(): number {
    const selected = this.essenceState.selectedInventoryItem();
    return selected ? this.spareCopies(selected) : 0;
  }

  selectedDustPerCopy(): number {
    const selected = this.essenceState.selectedInventoryItem();
    if (!selected) return 0;

    return Math.max(
      1,
      (selected.itemInstance.itemBase as EssenceItem).dismantleDustAmount,
    );
  }

  setSingleShatterQuantity(event: Event): void {
    this.setClampedSingleShatterQuantity(
      Number((event.target as HTMLInputElement).value),
    );
  }

  adjustSingleShatterQuantity(change: number): void {
    this.setClampedSingleShatterQuantity(this.singleShatterQuantity() + change);
  }

  maximizeSingleShatterQuantity(): void {
    this.setClampedSingleShatterQuantity(this.selectedSpareCopies());
  }

  canShatterSelected(): boolean {
    const selected = this.essenceState.selectedInventoryItem();
    return !!selected && this.spareCopies(selected) > 0;
  }

  isSelectedForShatter(inventoryItem: InventoryItem): boolean {
    return this.selectedShatterIds().has(inventoryItem.itemInstance.id);
  }

  toggleShatterSelection(inventoryItem: InventoryItem, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedShatterIds.update((current) => {
      const next = new Set(current);
      if (checked) next.add(inventoryItem.itemInstance.id);
      else next.delete(inventoryItem.itemInstance.id);
      return next;
    });
  }

  toggleAbsorbedDuplicates(event: Event): void {
    this.setAbsorbedDuplicatesSelected(
      (event.target as HTMLInputElement).checked,
    );
  }

  toggleMobileDuplicatesSelection(): void {
    this.setAbsorbedDuplicatesSelected(!this.allAbsorbedDuplicatesSelected());
  }

  private setAbsorbedDuplicatesSelected(checked: boolean): void {
    this.selectedShatterIds.update((current) => {
      const next = new Set(current);
      for (const item of this.absorbedDuplicateCandidates()) {
        if (checked) next.add(item.itemInstance.id);
        else next.delete(item.itemInstance.id);
      }
      return next;
    });
  }

  clearShatterSelection(): void {
    this.selectedShatterIds.set(new Set());
  }

  trackInventoryEssence(_: number, inventoryItem: InventoryItem): string {
    return inventoryItem.itemInstance.id;
  }

  absorb(): void {
    this.essenceState.absorbSelectedInventoryEssence()?.subscribe();
  }

  confirmShatter(): void {
    if (this.shattering()) return;

    const operation =
      this.shatterMode === 'bulk'
        ? this.essenceState.dismantleInventoryEssences(
            this.selectedShatterItems().map((item) => ({
              inventoryItemId: item.itemInstance.id,
              quantity: this.spareCopies(item),
            })),
          )
        : this.essenceState.dismantleSelectedInventoryEssence(
            this.singleShatterQuantity(),
          );
    if (!operation) return;

    this.shattering.set(true);
    operation.subscribe({
      next: (response) => {
        if (!response.succeeded) return;
        if (this.shatterMode === 'bulk') {
          this.clearShatterSelection();
          this.mobileSelectionMode.set(false);
        }
        this.closeModal();
      },
      complete: () => this.shattering.set(false),
    });
  }

  openModal(): void {
    this.captureModalTrigger();
    this.setClampedSingleShatterQuantity(this.singleShatterQuantity());
    this.shatterMode = 'single';
    this.showModal = true;
  }

  openBulkShatterModal(): void {
    if (this.selectedShatterCount() === 0) return;
    this.captureModalTrigger();
    this.shatterMode = 'bulk';
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    const target = this.modalTrigger;
    this.modalTrigger = null;
    queueMicrotask(() => target?.focus());
  }

  @HostListener('document:keydown.escape', ['$event'])
  closeModalOnEscape(event: KeyboardEvent): void {
    if (!this.showModal || this.shattering()) return;
    event.preventDefault();
    this.closeModal();
  }

  private captureModalTrigger(): void {
    this.modalTrigger =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;
  }

  private setClampedSingleShatterQuantity(quantity: number): void {
    const maximum = this.selectedSpareCopies();
    if (maximum <= 0) {
      this.singleShatterQuantity.set(1);
      return;
    }

    const normalized = Number.isFinite(quantity) ? Math.floor(quantity) : 1;
    this.singleShatterQuantity.set(Math.min(maximum, Math.max(1, normalized)));
  }
}
