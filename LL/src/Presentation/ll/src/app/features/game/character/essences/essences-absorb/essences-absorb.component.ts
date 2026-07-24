import { NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { EssenceStateService } from '../../../../../core/services/api/essences/essence-state.service';
import { RegularButtonComponent } from '../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { EssenceDetailsComponent } from '../../../../../shared/components/essences/essence-details/essence-details.component';
import { FilterOption } from '../../../../../shared/components/list-filters/list-filter/filter-option';
import { SelectableListFilterComponent } from '../../../../../shared/components/list-filters/selectable-list-filter/selectable-list-filter.component';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';

@Component({
    selector: 'app-essences-absorb',
    imports: [
        NgIf,
        ReactiveFormsModule,
        FormsModule,
        RegularButtonComponent,
        EssenceDetailsComponent,
        SelectableListFilterComponent,
    ],
    templateUrl: './essences-absorb.component.html'
})
export class EssencesAbsorbComponent {
  showModal = false;

  readonly essenceFilters: FilterOption<InventoryItem>[] = [
    {
      label: 'All',
      predicate: () => true,
    },
    {
      label: 'Already absorbed',
      predicate: (item) => this.essenceState.isInventoryEssenceAbsorbed(item),
    },
    {
      label: 'Not absorbed',
      predicate: (item) => !this.essenceState.isInventoryEssenceAbsorbed(item),
    },
  ];

  constructor(public readonly essenceState: EssenceStateService) {}

  selectEssence(inventoryItem: InventoryItem): void {
    this.essenceState.selectInventoryEssence(inventoryItem);
  }

  absorb(): void {
    this.essenceState.absorbSelectedInventoryEssence()?.subscribe();
  }

  confirmShatter(): void {
    this.essenceState.dismantleSelectedInventoryEssence()?.subscribe((response) => {
      if (response.succeeded) this.closeModal();
    });
  }

  openModal(): void {
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }
}
