import { Component, computed, OnInit } from '@angular/core';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { Essence } from '../../../../shared/models/essence';
import { EssencesService } from '../../../../core/services/api/essences/essences.service';
import { ModalService } from '../../../../core/services/client-side/modal/modal.service';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { EssenceItem } from '../../../../shared/models/item';
import { Tab } from '../../../../shared/models/sidebar-item';

@Component({
  selector: 'app-temple',
  standalone: true,
  imports: [ButtonComponent, BannerComponent],
  templateUrl: './temple.component.html',
})
export class TempleComponent implements OnInit {
  // public equippedEssences: Essence[] = [];
  public inventoryEssences: Essence[] = [];
  readonly equippedEssences = computed(() =>
    this.inventoryState
      .items()
      .filter((i) => i.itemInstance.itemBase.itemType === ItemType.Essence)
      .map((i) => i.itemInstance),
  );

  constructor(
    private modalService: ModalService,
    private essencesService: EssencesService,
    private inventoryState: InventoryStateService,
  ) {}

  ngOnInit(): void {
    this.setActiveTab(this.tabs[0]?.label || '');
  }

  equipEssence() {
    this.essencesService.equipEssence('00000000-0000-0000-0000-000000000001');
  }

  openAbsorbEssenceModal() {
    const filteredEssences = this.inventoryEssences.filter(
      (essence) =>
        !this.equippedEssences().some(
          (equipped) =>
            (equipped.itemBase as EssenceItem).essence.name === essence.name,
        ),
    );
    this.modalService.toggleAbsorbEssenceModal(filteredEssences);
  }

  // openRemoveEssenceModal() {
  //   this.modalService.toggleRemoveEssenceModal(this.equippedEssences);
  // }

  tabs: Tab[] = [
    {
      label: 'Buy',
      items: [],
    },
    {
      label: 'Sell',
      items: [],
    },
  ];
  activeTab: string = '';

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }
}
