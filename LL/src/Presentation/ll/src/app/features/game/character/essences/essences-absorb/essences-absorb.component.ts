import { Component, computed, Input, Signal, signal } from '@angular/core';
import { EssenceStateService } from '../../../../../core/services/api/essences/essence-state.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { Essence } from '../../../../../shared/models/essence';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { EssenceItem } from '../../../../../shared/models/item';
import { NgFor, NgIf, NgClass } from '@angular/common';
import { RegularButtonComponent } from '../../../../../shared/components/buttons/regular-button/regular-button.component';
import { EssenceDetailsComponent } from '../../../../../shared/components/essences/essence-details/essence-details.component';

@Component({
  selector: 'app-essences-absorb',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    NgClass,
    RegularButtonComponent,
    EssenceDetailsComponent,
  ],
  templateUrl: './essences-absorb.component.html',
})
export class EssencesAbsorbComponent {
  @Input({ required: true }) inventoryEssences!: Signal<InventoryItem[]>;

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

  constructor(
    private essenceState: EssenceStateService,
    private inventoryState: InventoryStateService,
  ) {}

  selectEssence(inventoryItem: InventoryItem): void {
    this.selectedItemInstanceId.set(inventoryItem.itemInstance.id);
  }

  canEquipSelected(): boolean {
    return true;
  }

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
}
