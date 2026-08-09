import { NgClass, NgFor, NgIf } from '@angular/common';
import {
  Component,
  computed,
  effect,
  OnInit,
  signal,
  untracked,
} from '@angular/core';
import { SidebarSection } from '../../../../shared/models/sidebar-item';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { EquipmentOverviewComponent } from '../../../../shared/components/equipment-overview/equipment-overview.component';
import { InventoryItemComponent } from '../../../../shared/components/inventory-item/inventory-item.component';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { FilterTabsComponent } from '../../../../shared/components/custom-components/tabs/filter-tabs/filter-tabs.component';
import { RegularButtonComponent } from '../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { EquipmentInstance } from '../../../../shared/models/item';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { EquipmentType } from '../../../../shared/models/enums/equipmentType';
import { Rarity } from '../../../../shared/models/enums/rarity';
import { FormsModule } from '@angular/forms';
import { ItemComponent } from '../../../../shared/components/item/item.component';
import { HelpTooltipDirective } from '../../../../shared/help/help-tooltip.directive';
import { EquipmentTypePipe } from '../../../../shared/pipes/equipment/equipment-type-format/equipment-type.pipe';
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../../../shared/components/custom-components/dropdown/dropdown.component';
import { TutorialStateService } from '../../../../core/services/api/tutorial/tutorial-state.service';
import { TutorialPresenterService } from '../../../../core/services/api/tutorial/tutorial-presenter.service';
import {
  TUTORIAL_GATHERING_TOOL_ITEM_BASE_IDS,
  TUTORIAL_STEP_EQUIP_EQUIPMENT,
  TUTORIAL_STEP_EQUIP_GATHERING_TOOL,
  TUTORIAL_ONE_HANDED_WEAPON_ITEM_BASE_IDS,
} from '../../../../shared/models/tutorial';
type MobileInventoryView = 'Inventory' | 'Equipment';

@Component({
  selector: 'app-inventory',
  imports: [
    NgFor,
    NgIf,
    NgClass,
    FilterTabsComponent,
    InventoryItemComponent,
    DefaultHeaderComponent,
    EquipmentOverviewComponent,
    RegularButtonComponent,
    FormsModule,
    ItemComponent,
    HelpTooltipDirective,
    EquipmentTypePipe,
    DropdownComponent,
  ],
  templateUrl: './inventory.component.html',
})
export class InventoryComponent implements OnInit {
  readonly mobileView = signal<MobileInventoryView>('Inventory');
  readonly mobileViewTabs: readonly MobileInventoryView[] = [
    'Inventory',
    'Equipment',
  ];

  tabs: SidebarSection[] = [
    {
      id: 'all',
      label: 'All',
      items: [],
    },
    {
      id: 'equipment',
      label: 'Equipment',
      items: [],
    },
    {
      id: 'resources',
      label: 'Resources',
      items: [],
    },
    {
      id: 'essences',
      label: 'Essences',
      items: [],
    },
  ];
  activeTab: string = '';

  inventoryMode: 'Scrap Mode' | 'Regular Mode' = 'Regular Mode';

  temperedItems = computed(() => {
    return this.state
      .equipment()
      .filter((i) => (i.itemInstance as EquipmentInstance).potential === 0);
  });

  selectedItems: InventoryItem[] = [];
  scrapRarityThreshold: Rarity = Rarity.Common;
  rarities = Object.keys(Rarity);
  rarityDropdownOptions: DropdownOption<Rarity>[] = this.rarities.map(
    (rarity) => ({
      label: rarity,
      value: rarity as Rarity,
    }),
  );
  RARITY_ORDER: Record<Rarity, number> = {
    [Rarity.Common]: 0,
    [Rarity.Uncommon]: 1,
    [Rarity.Rare]: 2,
    [Rarity.Epic]: 3,
    [Rarity.Unique]: 4,
    [Rarity.Legendary]: 5,
    [Rarity.Legacy]: 6,
  };

  constructor(
    public state: InventoryStateService,
    private readonly tutorialState: TutorialStateService,
    private readonly tutorialPresenter: TutorialPresenterService,
  ) {
    effect(() => {
      const tutorial = this.tutorialState.state();
      if (
        (tutorial?.currentStep === TUTORIAL_STEP_EQUIP_EQUIPMENT ||
          tutorial?.currentStep === TUTORIAL_STEP_EQUIP_GATHERING_TOOL) &&
        !tutorial.isCompleted
      ) {
        this.enterBrowseMode();

        if (tutorial.currentStep === TUTORIAL_STEP_EQUIP_GATHERING_TOOL) {
          untracked(() => this.tutorialPresenter.presentCurrentStep());
        }
      }
    });
  }

  ngOnInit(): void {
    this.state.load();

    this.setActiveTab(this.tabs[0]?.label || '');
  }

  toggleSelectItem(selectedItem: InventoryItem) {
    if (this.selectedItems.includes(selectedItem)) {
      this.selectedItems = this.selectedItems.filter((item) => {
        return item.itemInstance.id !== selectedItem.itemInstance.id;
      });
    } else {
      this.selectedItems.push(selectedItem);
    }
  }

  cancelScrapMode() {
    this.selectedItems = [];
    this.enterBrowseMode();
  }

  selectAllTempered() {
    this.selectedItems = [];
    this.temperedItems().forEach((item) => this.selectedItems.push(item));
  }

  selectAllBelowRarity() {
    const thresholdRank = this.RARITY_ORDER[this.scrapRarityThreshold];

    this.selectedItems = [];
    this.temperedItems()
      .filter((item) => {
        const itemRank =
          this.RARITY_ORDER[(item.itemInstance as EquipmentInstance).rarity];
        return itemRank <= thresholdRank;
      })
      .forEach((item) => this.selectedItems.push(item));
  }

  setScrapRarityThreshold(selection: DropdownSelection<unknown>) {
    this.scrapRarityThreshold = selection.main as Rarity;
  }

  clearSelection() {
    this.selectedItems = [];
  }

  scrapEquipment() {
    this.state.scrapEquipment(this.selectedItems.map((i) => i.itemInstance.id));
    this.selectedItems = [];
  }

  switchMode() {
    if (this.isScrapMode) {
      this.enterBrowseMode();
    } else {
      this.enterScrapMode();
    }
  }

  enterBrowseMode() {
    this.selectedItems = [];
    this.inventoryMode = 'Regular Mode';
  }

  enterScrapMode() {
    this.selectedItems = [];
    this.inventoryMode = 'Scrap Mode';
  }

  selectedItemsContains(item: InventoryItem) {
    return !!this.selectedItems.find(
      (i) => i.itemInstance.id === item.itemInstance.id,
    );
  }

  isEquipmentItem(item: InventoryItem): boolean {
    return item.itemInstance.itemBase.itemType === ItemType.Equipment;
  }

  equipmentInstance(item: InventoryItem): EquipmentInstance | null {
    return this.isEquipmentItem(item)
      ? (item.itemInstance as EquipmentInstance)
      : null;
  }

  isTutorialCraftedWeapon(item: InventoryItem): boolean {
    const equipment = this.equipmentInstance(item);
    return (
      equipment?.tier === 1 &&
      !!equipment.baseRecipeId &&
      TUTORIAL_ONE_HANDED_WEAPON_ITEM_BASE_IDS.has(equipment.itemBase.id)
    );
  }

  isTutorialGatheringTool(item: InventoryItem): boolean {
    const equipment = this.equipmentInstance(item);
    return (
      equipment?.equipmentBase.equipmentType === EquipmentType.Tool &&
      TUTORIAL_GATHERING_TOOL_ITEM_BASE_IDS.has(equipment.itemBase.id)
    );
  }

  equipmentRarityClass(item: InventoryItem): string {
    const rarity = this.equipmentInstance(item)?.rarity ?? Rarity.Common;

    switch (rarity) {
      case Rarity.Common:
        return 'll-rarity-common';
      case Rarity.Uncommon:
        return 'll-rarity-uncommon';
      case Rarity.Rare:
        return 'll-rarity-rare';
      case Rarity.Epic:
        return 'll-rarity-epic';
      case Rarity.Unique:
        return 'll-rarity-unique';
      case Rarity.Legendary:
        return 'll-rarity-legendary';
      case Rarity.Legacy:
        return 'll-rarity-legacy';
      default:
        return 'border-light_gray/60 text-secondary';
    }
  }

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }

  selectMobileView(view: string): void {
    if (view === 'Inventory' || view === 'Equipment') {
      this.mobileView.set(view);
    }
  }

  get filteredItems(): InventoryItem[] {
    switch (this.isBrowseMode ? this.activeTab : 'Equipment') {
      case 'All':
        return this.state.items();

      case 'Equipment':
        return this.isBrowseMode
          ? this.state.equipment()
          : this.temperedItems();

      case 'Resources':
        return this.sortResourcesForDisplay(this.state.materials());

      case 'Essences':
        return this.state.essences();

      default:
        return this.state.items();
    }
  }

  get tabLabels(): string[] {
    return this.isBrowseMode
      ? this.tabs.map((tab) => tab.label)
      : ['Equipment'];
  }

  get isBrowseMode(): boolean {
    return this.inventoryMode === 'Regular Mode';
  }

  get isScrapMode(): boolean {
    return this.inventoryMode === 'Scrap Mode';
  }

  get selectedItemCountLabel(): string {
    return `${this.selectedItems.length} item${this.selectedItems.length === 1 ? '' : 's'}`;
  }

  get inventoryCountLabel(): string {
    const count = this.state.items().length;
    return `${count} item${count === 1 ? '' : 's'}`;
  }

  get activeListTitle(): string {
    return 'Tempered Equipment';
  }

  get activeListDescription(): string {
    return 'Only equipment with 0 potential can be turned into tempered scrap.';
  }

  get emptyStateText(): string {
    return this.isScrapMode
      ? 'No tempered equipment is ready to scrap.'
      : 'No items in this category.';
  }

  private sortResourcesForDisplay(items: InventoryItem[]): InventoryItem[] {
    return [...items].sort((a, b) => {
      const blueprintRank =
        Number(this.isBlueprintResource(b)) -
        Number(this.isBlueprintResource(a));
      if (blueprintRank !== 0) return blueprintRank;

      return a.itemInstance.itemBase.name.localeCompare(
        b.itemInstance.itemBase.name,
      );
    });
  }

  private isBlueprintResource(item: InventoryItem): boolean {
    const itemBase = item.itemInstance.itemBase;
    return (
      itemBase.itemType === ItemType.Resource &&
      (itemBase.id.toLowerCase().startsWith('blueprint_') ||
        itemBase.name.toLowerCase().startsWith('blueprint:'))
    );
  }
}
