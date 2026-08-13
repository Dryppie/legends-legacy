import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
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
import { ItemQuality } from '../../../../shared/models/enums/itemQuality';
import { FormsModule } from '@angular/forms';
import { ItemComponent } from '../../../../shared/components/item/item.component';
import { HelpTooltipDirective } from '../../../../shared/help/help-tooltip.directive';
import { EquipmentTypePipe } from '../../../../shared/pipes/equipment/equipment-type-format/equipment-type.pipe';
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../../../shared/components/custom-components/dropdown/dropdown.component';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import { QuestPresenterService } from '../../../../core/services/api/quest/quest-presenter.service';
import {
  ONBOARDING_GATHERING_TOOL_ITEM_BASE_IDS,
  ONBOARDING_ONE_HANDED_WEAPON_ITEM_BASE_IDS,
} from '../../../../shared/models/quest';
type MobileInventoryView = 'Inventory' | 'Equipment';
type InventorySort =
  | 'Default'
  | 'Name'
  | 'Tier'
  | 'Rarity'
  | 'Quality'
  | 'Gear Value';

@Component({
  selector: 'app-inventory',
  imports: [
    NgFor,
    NgIf,
    NgClass,
    DecimalPipe,
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

  scrapableEquipment = computed(() =>
    this.state
      .equipment()
      .filter(
        (item) =>
          !(item.itemInstance as EquipmentInstance).isGuildBorrowed &&
          (item.itemInstance as EquipmentInstance).equipmentBase
            .equipmentType !== EquipmentType.Tool,
      ),
  );

  selectedItems: InventoryItem[] = [];
  scrapRarityThreshold: Rarity = Rarity.Common;
  inventorySort: InventorySort = 'Default';
  readonly sortDropdownOptions: DropdownOption<InventorySort>[] = [
    { label: 'Default', value: 'Default' },
    { label: 'Name A-Z', value: 'Name' },
    { label: 'Tier: high to low', value: 'Tier' },
    { label: 'Rarity: high to low', value: 'Rarity' },
    { label: 'Quality: high to low', value: 'Quality' },
    { label: 'Gear Value: high to low', value: 'Gear Value' },
  ];
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
  readonly QUALITY_ORDER: Record<ItemQuality, number> = {
    [ItemQuality.Crude]: 0,
    [ItemQuality.Standard]: 1,
    [ItemQuality.Fine]: 2,
    [ItemQuality.Exceptional]: 3,
    [ItemQuality.Masterwork]: 4,
  };

  constructor(
    public state: InventoryStateService,
    private readonly questState: QuestStateService,
    private readonly questPresenter: QuestPresenterService,
  ) {
    effect(() => {
      const objectiveType = this.questState.pinnedObjective()?.type;
      if (
        objectiveType === 'EquipmentEquipped' ||
        objectiveType === 'GatheringToolEquipped'
      ) {
        this.enterBrowseMode();

        if (objectiveType === 'GatheringToolEquipped') {
          untracked(() => this.questPresenter.presentCurrentObjective());
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

  selectAllEquipment() {
    this.selectedItems = [];
    this.scrapableEquipment().forEach((item) => this.selectedItems.push(item));
  }

  selectAllBelowRarity() {
    const thresholdRank = this.RARITY_ORDER[this.scrapRarityThreshold];

    this.selectedItems = [];
    this.scrapableEquipment()
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

  setInventorySort(selection: DropdownSelection<unknown>): void {
    this.inventorySort = selection.main as InventorySort;
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

  isOnboardingCraftedWeapon(item: InventoryItem): boolean {
    const equipment = this.equipmentInstance(item);
    return (
      equipment?.tier === 1 &&
      !!equipment.baseRecipeId &&
      ONBOARDING_ONE_HANDED_WEAPON_ITEM_BASE_IDS.has(equipment.itemBase.id)
    );
  }

  isOnboardingGatheringTool(item: InventoryItem): boolean {
    const equipment = this.equipmentInstance(item);
    return (
      equipment?.equipmentBase.equipmentType === EquipmentType.Tool &&
      ONBOARDING_GATHERING_TOOL_ITEM_BASE_IDS.has(equipment.itemBase.id)
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
    let items: InventoryItem[];
    switch (this.isBrowseMode ? this.activeTab : 'Equipment') {
      case 'All':
        items = this.state.items();
        break;

      case 'Equipment':
        items = this.isBrowseMode
          ? this.state.equipment()
          : this.scrapableEquipment();
        break;

      case 'Resources':
        items = this.sortResourcesForDisplay(this.state.materials());
        break;

      case 'Essences':
        items = this.state.essences();
        break;

      default:
        items = this.state.items();
        break;
    }

    return this.sortInventoryItems(items);
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
    return 'Equipment';
  }

  get activeListDescription(): string {
    return 'Any unequipped non-tool equipment can be turned into tempered scrap.';
  }

  get emptyStateText(): string {
    return this.isScrapMode
      ? 'No equipment is ready to scrap.'
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

  private sortInventoryItems(items: InventoryItem[]): InventoryItem[] {
    if (this.inventorySort === 'Default') return [...items];

    return [...items].sort((a, b) => {
      const aEquipment = this.equipmentInstance(a);
      const bEquipment = this.equipmentInstance(b);
      let difference = 0;

      switch (this.inventorySort) {
        case 'Tier':
          difference = (bEquipment?.tier ?? 0) - (aEquipment?.tier ?? 0);
          break;
        case 'Rarity':
          difference =
            this.RARITY_ORDER[
              bEquipment?.rarity ?? b.itemInstance.itemBase.rarity
            ] -
            this.RARITY_ORDER[
              aEquipment?.rarity ?? a.itemInstance.itemBase.rarity
            ];
          break;
        case 'Quality':
          difference =
            (bEquipment ? this.QUALITY_ORDER[bEquipment.quality] : -1) -
            (aEquipment ? this.QUALITY_ORDER[aEquipment.quality] : -1);
          break;
        case 'Gear Value':
          difference =
            (bEquipment?.itemBudget ?? 0) - (aEquipment?.itemBudget ?? 0);
          break;
      }

      return (
        difference ||
        this.itemDisplayName(a).localeCompare(this.itemDisplayName(b))
      );
    });
  }

  private itemDisplayName(item: InventoryItem): string {
    return (
      this.equipmentInstance(item)?.displayName ??
      item.itemInstance.itemBase.name
    );
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
