import { Component, computed } from '@angular/core';
import { EssenceStateService } from '../../../../core/services/api/essences/essence-state.service';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { TabComponent } from '../../../../shared/components/tabs/tab/tab.component';
import { TabsComponent } from '../../../../shared/components/tabs/tabs.component';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { EssencesEquippedComponent } from './essences-equipped/essences-equipped.component';
import { EssencesAbsorbComponent } from './essences-absorb/essences-absorb.component';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { SlotState } from '../../../../shared/models/essenceSlot';

@Component({
  selector: 'app-essences',
  standalone: true,
  imports: [
    TabsComponent,
    TabComponent,
    EssencesEquippedComponent,
    EssencesAbsorbComponent,
    DefaultHeaderComponent,
  ],
  templateUrl: './essences.component.html',
})
export class EssencesComponent {
  public essenceSlots = computed(() => this.essenceState.essenceSlots());

  public inventoryEssences = computed(() => {
    if (this.essenceState.loading()) return []; // keeps it from race condition display of all inventory essences if essenceSlots loads slowly
    return this.inventoryState.items().filter(
      (i) => i.itemInstance.itemBase.itemType === ItemType.Essence,
      // &&
      //   !this.essenceSlots()
      //     .filter((es) => es.occupiedEssence !== null)
      //     .map((es) => es.occupiedEssence?.id)
      //     .includes((i.itemInstance.itemBase as EssenceItem).essence.id),
    );
  });

  public numberOfAbsorbedEssence = computed(
    () =>
      `${
        this.essenceSlots().filter(
          (es) =>
            es.occupiedEssence !== null && es.slotState === SlotState.Active,
        ).length
      }/${this.essenceSlots().filter((es) => es.slotState === SlotState.Active).length} essence absorbed`,
  );

  constructor(
    private essenceState: EssenceStateService,
    private inventoryState: InventoryStateService,
  ) {}
}
