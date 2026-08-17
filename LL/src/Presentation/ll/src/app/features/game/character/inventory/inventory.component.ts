import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import {
  Component,
  computed,
  effect,
  OnInit,
  signal,
  untracked,
} from '@angular/core';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { EquipmentOverviewComponent } from '../../../../shared/components/equipment-overview/equipment-overview.component';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { FilterTabsComponent } from '../../../../shared/components/custom-components/tabs/filter-tabs/filter-tabs.component';
import { RegularButtonComponent } from '../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import {
  EquipmentInstance,
  SelectionCrateOption,
} from '../../../../shared/models/item';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { EquipmentType } from '../../../../shared/models/enums/equipmentType';
import { Rarity } from '../../../../shared/models/enums/rarity';
import { ItemQuality } from '../../../../shared/models/enums/itemQuality';
import { FormsModule } from '@angular/forms';
import { ItemComponent } from '../../../../shared/components/item/item.component';
import { HelpTooltipDirective } from '../../../../shared/help/help-tooltip.directive';
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
import { EquipmentDisplayComponent } from '../../../../shared/components/equipment/equipment-display/equipment-display.component';
import { ModalService } from '../../../../core/services/client-side/modal/modal.service';
import { EquipmentStateService } from '../../../../core/services/api/equipment/equipment-state.service';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import {
  getAllowedEquipmentTypesForSlot,
  getEquipSlotOptions,
  getSlotTypeFromEquipmentType,
} from '../../../../shared/utils/equipment/equipment.utils';
import {
  isMarketplaceBlueprintResource,
  MARKETPLACE_CATALYST_ITEM_IDS,
} from '../../../../shared/utils/market-place/market-place-category.utils';
import { InventoryTransferComponent } from '../../../../shared/components/inventory-transfer/inventory-transfer.component';
import { BlueprintAttributeSummaryComponent } from '../../../../shared/components/blueprint-attribute-summary/blueprint-attribute-summary.component';
import { CraftingService } from '../../../../core/services/api/crafting/crafting.service';
import { InventoryService } from '../../../../core/services/api/inventory/inventory.service';
import { GuildStateService } from '../../../../core/services/api/guild/guild-state.service';
import { finalize } from 'rxjs';
type InventoryCollectionView = 'Equipment' | 'Stock';
type StockCategory =
  | 'Resources'
  | 'Essences'
  | 'Blueprints'
  | 'Entrance Keys'
  | 'Catalysts';
type InventorySort = 'Name' | 'Tier' | 'Rarity' | 'Quality' | 'Gear Power';
type EquipmentInventorySort = 'Name' | 'Quality' | 'Potential' | 'Gear Power';
type SortDirection = 'asc' | 'desc';

@Component({
  selector: 'app-inventory',
  imports: [
    NgFor,
    NgIf,
    NgClass,
    DecimalPipe,
    FilterTabsComponent,
    DefaultHeaderComponent,
    EquipmentOverviewComponent,
    EquipmentDisplayComponent,
    RegularButtonComponent,
    FormsModule,
    ItemComponent,
    HelpTooltipDirective,
    DropdownComponent,
    InventoryTransferComponent,
    BlueprintAttributeSummaryComponent,
  ],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.scss',
})
export class InventoryComponent implements OnInit {
  readonly collectionView = signal<InventoryCollectionView>('Equipment');
  readonly stockCategory = signal<StockCategory>('Resources');
  readonly stockCategories: readonly StockCategory[] = [
    'Resources',
    'Essences',
    'Blueprints',
    'Entrance Keys',
    'Catalysts',
  ];
  activeTab: string = '';

  inventoryMode: 'Scrap Mode' | 'Regular Mode' = 'Regular Mode';
  inventorySearch = '';
  readonly selectedItem = signal<InventoryItem | null>(null);
  readonly mobileItemInspectorOpen = signal(false);
  readonly selectedBlueprintRecipeId = signal('');
  readonly blueprintRecipeOptions = signal<readonly DropdownOption<string>[]>(
    [],
  );
  readonly isLoadingBlueprintRecipes = signal(false);
  readonly hasLoadedBlueprintRecipes = signal(false);
  readonly isLearningBlueprint = signal(false);
  readonly blueprintActionError = signal<string | null>(null);
  readonly selectedContainerOptionId = signal('');
  readonly isOpeningContainer = signal(false);
  readonly containerActionError = signal<string | null>(null);
  readonly favoritePendingItemId = signal<string | null>(null);
  readonly favoriteActionError = signal<string | null>(null);
  readonly donationPendingItemId = signal<string | null>(null);
  readonly donationActionError = signal<string | null>(null);
  readonly selectedEquipmentSlot = signal<EquipmentSlotType | null>(null);
  readonly selectedSlotEquipment = signal<EquipmentInstance | null>(null);

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
  inventorySort: EquipmentInventorySort = 'Gear Power';
  inventorySortDirection: SortDirection = 'desc';
  stockSort: InventorySort = 'Name';
  readonly sortDropdownOptions: DropdownOption<InventorySort>[] = [
    { label: 'Name A-Z', value: 'Name' },
    { label: 'Tier: high to low', value: 'Tier' },
    { label: 'Rarity: high to low', value: 'Rarity' },
    { label: 'Quality: high to low', value: 'Quality' },
    { label: 'Gear Power: high to low', value: 'Gear Power' },
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
    private readonly modalService?: ModalService,
    private readonly equipmentState?: EquipmentStateService,
    private readonly craftingService?: CraftingService,
    private readonly inventoryService?: InventoryService,
    private readonly guildState?: GuildStateService,
  ) {
    effect(() => {
      const objectiveType = this.questState.pinnedOnboardingObjective()?.type;
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

    this.setActiveTab('Equipment');
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

  setInventorySort(sort: EquipmentInventorySort): void {
    if (this.inventorySort === sort) {
      this.inventorySortDirection =
        this.inventorySortDirection === 'asc' ? 'desc' : 'asc';
      return;
    }

    this.inventorySort = sort;
    this.inventorySortDirection = sort === 'Name' ? 'asc' : 'desc';
  }

  inventorySortIndicator(sort: EquipmentInventorySort): string {
    if (this.inventorySort !== sort) return '';
    return this.inventorySortDirection === 'asc' ? '↑' : '↓';
  }

  inventoryAriaSort(
    sort: EquipmentInventorySort,
  ): 'ascending' | 'descending' | 'none' {
    if (this.inventorySort !== sort) return 'none';
    return this.inventorySortDirection === 'asc' ? 'ascending' : 'descending';
  }

  setStockSort(selection: DropdownSelection<unknown>): void {
    this.stockSort = selection.main as InventorySort;
  }

  clearSelection() {
    this.selectedItems = [];
  }

  scrapEquipment() {
    this.state.scrapEquipment(this.selectedItems.map((i) => i.itemInstance.id));
    this.selectedItems = [];
  }

  canScrapItem(item: InventoryItem): boolean {
    return this.scrapableEquipment().some(
      (candidate) => candidate.itemInstance.id === item.itemInstance.id,
    );
  }

  beginScrappingItem(item: InventoryItem): void {
    if (!this.canScrapItem(item)) return;

    this.enterScrapMode();
    this.selectedItems = [item];
  }

  canDonateToGuild(item: InventoryItem): boolean {
    const equipment = this.equipmentInstance(item);
    return (
      !!this.guildState?.isInGuild() &&
      !!equipment &&
      !equipment.isGuildBorrowed &&
      !this.isSelectedEquippedItem(item)
    );
  }

  donateToGuild(item: InventoryItem): void {
    if (
      this.donationPendingItemId() !== null ||
      !this.guildState ||
      !this.canDonateToGuild(item)
    ) {
      return;
    }

    const itemInstanceId = item.itemInstance.id;
    this.donationPendingItemId.set(itemInstanceId);
    this.donationActionError.set(null);

    this.guildState
      .donateVaultItem(itemInstanceId)
      .pipe(finalize(() => this.donationPendingItemId.set(null)))
      .subscribe({
        next: () => {
          if (this.selectedItem()?.itemInstance.id === itemInstanceId) {
            this.clearSelectedItem();
          }
        },
        error: (error) => {
          if (this.selectedItem()?.itemInstance.id === itemInstanceId) {
            this.donationActionError.set(
              error?.message ?? 'Failed to donate this item to the guild.',
            );
          }
        },
      });
  }

  handleSelectedItemTransferred(item: InventoryItem): void {
    const remainingItem = this.state
      .items()
      .find((candidate) => candidate.itemInstance.id === item.itemInstance.id);
    if (remainingItem) {
      this.selectedItem.set(remainingItem);
    } else {
      this.clearSelectedItem();
    }
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
    this.clearEquipmentSlotFilter();
    this.collectionView.set('Equipment');
    this.setActiveTab('Equipment');
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
    this.clearSelectedItem();
  }

  selectCollectionView(view: InventoryCollectionView): void {
    this.collectionView.set(view);
    this.clearEquipmentSlotFilter();
    this.setActiveTab(view === 'Equipment' ? 'Equipment' : 'All');
  }

  selectEquipmentSlot(slot: EquipmentSlot): void {
    this.mobileItemInspectorOpen.set(false);
    if (this.selectedEquipmentSlot() === slot.equipmentSlotType) {
      this.clearEquipmentSlotFilter();
      return;
    }

    this.selectedEquipmentSlot.set(slot.equipmentSlotType);
    this.selectedSlotEquipment.set(slot.equipmentInstance ?? null);
    this.selectedItem.set(
      slot.equipmentInstance
        ? this.inventoryItemForEquipment(slot.equipmentInstance)
        : null,
    );
  }

  clearEquipmentSlotFilter(): void {
    this.selectedEquipmentSlot.set(null);
    this.selectedSlotEquipment.set(null);
    this.clearSelectedItem();
  }

  selectStockCategory(category: StockCategory): void {
    this.stockCategory.set(category);
    this.clearSelectedItem();
  }

  get filteredItems(): InventoryItem[] {
    let items = this.isBrowseMode
      ? this.state.equipment()
      : this.scrapableEquipment();

    const selectedSlot = this.selectedEquipmentSlot();
    if (this.isBrowseMode && selectedSlot) {
      const allowedTypes = getAllowedEquipmentTypesForSlot(selectedSlot);
      items = items.filter((item) => {
        const equipment = this.equipmentInstance(item);
        return (
          !!equipment &&
          allowedTypes.includes(equipment.equipmentBase.equipmentType)
        );
      });
    }

    const query = this.inventorySearch.trim().toLowerCase();
    const filtered = query
      ? items.filter((item) => {
          const equipment = this.equipmentInstance(item);
          return [
            this.itemDisplayName(item),
            item.itemInstance.itemBase.itemType,
            item.itemInstance.itemBase.rarity,
            equipment?.equipmentBase.equipmentType,
            equipment?.rarity,
            equipment?.quality,
          ].some((value) => value?.toString().toLowerCase().includes(query));
        })
      : items;

    return this.sortInventoryItems(
      filtered,
      this.inventorySort,
      this.inventorySortDirection,
    );
  }

  get filteredStockItems(): InventoryItem[] {
    const query = this.inventorySearch.trim().toLowerCase();
    const items = this.stockItemsForCategory(this.stockCategory()).filter(
      (item) =>
        !query ||
        [
          this.itemDisplayName(item),
          item.itemInstance.itemBase.itemType,
          item.itemInstance.itemBase.rarity,
        ].some((value) => value?.toString().toLowerCase().includes(query)),
    );

    return this.sortInventoryItems(items, this.stockSort);
  }

  get tabLabels(): string[] {
    return [];
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

  get equipmentCount(): number {
    return this.state.equipment().length;
  }

  get stockCount(): number {
    return this.state.items().filter((item) => !this.isEquipmentItem(item))
      .length;
  }

  stockCategoryCount(category: StockCategory): number {
    return this.stockItemsForCategory(category).length;
  }

  get stockCategoryCountLabel(): string {
    const count = this.stockCategoryCount(this.stockCategory());
    return `${count} kind${count === 1 ? '' : 's'}`;
  }

  get activeListTitle(): string {
    const selectedSlot = this.selectedEquipmentSlot();
    if (selectedSlot && this.isBrowseMode) {
      return `${this.equipmentSlotLabel(selectedSlot)} equipment`;
    }

    if (this.isScrapMode || this.collectionView() === 'Equipment') {
      return 'Equipment';
    }

    return this.activeTab === 'All' ? 'Stock' : this.activeTab;
  }

  get activeListDescription(): string {
    return 'Any unequipped non-tool equipment can be turned into tempered scrap.';
  }

  get emptyStateText(): string {
    return this.isScrapMode
      ? 'No equipment is ready to scrap.'
      : 'No items in this category.';
  }

  selectInventoryItem(item: InventoryItem): void {
    const changedItem =
      this.selectedItem()?.itemInstance.id !== item.itemInstance.id;

    // Inspecting an item is what clears its "new" marker. Scrap-mode clicks never reach here,
    // so a bulk-scrap sweep leaves the badges alone.
    const inspected = item.isNew
      ? (this.state.markSeen(item.itemInstance.id) ?? item)
      : item;

    this.selectedItem.set(inspected);
    if (changedItem) {
      this.favoriteActionError.set(null);
      this.donationActionError.set(null);
      this.resetBlueprintAction();
      this.resetContainerAction(item);
      this.loadBlueprintRecipes(item);
    }
  }

  toggleFavorite(item: InventoryItem): void {
    if (this.favoritePendingItemId()) return;

    const itemInstanceId = item.itemInstance.id;
    this.favoritePendingItemId.set(itemInstanceId);
    this.favoriteActionError.set(null);

    const request = this.state.setFavorite(itemInstanceId, !item.isFavorite);
    this.refreshSelectedInventoryItem(itemInstanceId);

    request.subscribe({
      next: () => {
        this.refreshSelectedInventoryItem(itemInstanceId);
        this.favoritePendingItemId.set(null);
      },
      error: (error) => {
        this.refreshSelectedInventoryItem(itemInstanceId);
        this.favoriteActionError.set(
          error?.message ?? 'Failed to update this favorite.',
        );
        this.favoritePendingItemId.set(null);
      },
    });
  }

  private refreshSelectedInventoryItem(itemInstanceId: string): void {
    if (this.selectedItem()?.itemInstance.id !== itemInstanceId) return;

    const current = this.state
      .items()
      .find((item) => item.itemInstance.id === itemInstanceId);
    if (current) {
      this.selectedItem.set(current);
      return;
    }

    this.selectedItem.update((item) =>
      item
        ? { ...item, isFavorite: this.state.isFavorite(itemInstanceId) }
        : item,
    );
  }

  selectionContainerMetadata(item: InventoryItem) {
    return item.itemInstance.itemBase.selectionCrate ?? null;
  }

  selectContainerOption(option: SelectionCrateOption): void {
    this.selectedContainerOptionId.set(option.id);
    this.containerActionError.set(null);
  }

  openSelectionContainer(item: InventoryItem): void {
    const optionId = this.selectedContainerOptionId();
    if (
      !this.selectionContainerMetadata(item) ||
      !optionId ||
      !this.inventoryService ||
      this.isOpeningContainer()
    ) {
      return;
    }

    this.isOpeningContainer.set(true);
    this.containerActionError.set(null);
    this.inventoryService
      .openSelectionContainer(item.itemInstance.id, optionId)
      .subscribe({
        next: (response) => {
          this.state.decrementItem(response.consumedItemInstanceId, 1);
          this.state.applyInventoryGrant(response.grantId, response.rewards);
          this.isOpeningContainer.set(false);

          if (this.selectedItem()?.itemInstance.id !== item.itemInstance.id) {
            return;
          }

          const remainingItem = this.state
            .items()
            .find(
              (candidate) =>
                candidate.itemInstance.id === response.consumedItemInstanceId,
            );
          if (remainingItem) {
            this.selectedItem.set(remainingItem);
          } else {
            this.clearSelectedItem();
          }
        },
        error: (error) => {
          this.isOpeningContainer.set(false);
          if (this.selectedItem()?.itemInstance.id === item.itemInstance.id) {
            this.containerActionError.set(
              error.message ?? 'Failed to open this container.',
            );
          }
        },
      });
  }

  selectBlueprintRecipe(selection: DropdownSelection<unknown>): void {
    this.selectedBlueprintRecipeId.set(selection.main as string);
    this.blueprintActionError.set(null);
  }

  learnSelectedBlueprint(item: InventoryItem): void {
    const recipeId = this.selectedBlueprintRecipeId();
    const blueprint = this.blueprintMetadata(item);
    if (
      !blueprint ||
      !recipeId ||
      !this.craftingService ||
      this.isLearningBlueprint()
    ) {
      return;
    }

    this.isLearningBlueprint.set(true);
    this.blueprintActionError.set(null);
    this.craftingService
      .learnBlueprint(item.itemInstance.id, recipeId)
      .subscribe({
        next: () => {
          this.state.decrementItem(item.itemInstance.id, 1);
          const remainingItem = this.state
            .items()
            .find(
              (candidate) => candidate.itemInstance.id === item.itemInstance.id,
            );

          this.blueprintRecipeOptions.update((options) =>
            options.filter((option) => option.value !== recipeId),
          );
          this.selectedBlueprintRecipeId.set('');
          this.isLearningBlueprint.set(false);
          if (remainingItem) {
            this.selectedItem.set(remainingItem);
          } else {
            this.clearSelectedItem();
          }
        },
        error: (error) => {
          this.blueprintActionError.set(
            error.message ?? 'Failed to learn blueprint.',
          );
          this.isLearningBlueprint.set(false);
        },
      });
  }

  blueprintMetadata(item: InventoryItem) {
    return item.itemInstance.itemBase.blueprint ?? null;
  }

  equipmentSlotLabel(slot: EquipmentSlotType): string {
    switch (slot) {
      case EquipmentSlotType.MainHand:
        return 'Main hand';
      case EquipmentSlotType.OffHand:
        return 'Off hand';
      case EquipmentSlotType.Necklace:
        return 'Necklace';
      default:
        return slot;
    }
  }

  selectedItemContextLabel(item: InventoryItem): string {
    return this.isSelectedEquippedItem(item)
      ? 'Currently equipped'
      : 'Selected item';
  }

  isSelectedEquippedItem(item: InventoryItem): boolean {
    return this.equippedSlotTypeFor(item) !== null;
  }

  equippedSlotTypeFor(item: InventoryItem): EquipmentSlotType | null {
    const selectedSlot = this.selectedEquipmentSlot();
    if (
      selectedSlot &&
      this.selectedSlotEquipment()?.id === item.itemInstance.id
    ) {
      return selectedSlot;
    }

    return (
      this.equipmentState
        ?.equipmentSlots()
        .find((slot) => slot.equipmentInstance?.id === item.itemInstance.id)
        ?.equipmentSlotType ?? null
    );
  }

  comparisonEquipmentFor(item: InventoryItem): EquipmentInstance | null {
    const selectedSlot = this.selectedEquipmentSlot();
    const equipped = selectedSlot
      ? (this.equipmentState?.getSlot(selectedSlot)?.equipmentInstance ??
        this.selectedSlotEquipment())
      : this.selectedSlotEquipment();
    return equipped && equipped.id !== item.itemInstance.id ? equipped : null;
  }

  gearPowerDifference(item: InventoryItem): number | null {
    const equipment = this.equipmentInstance(item);
    const equipped = this.comparisonEquipmentFor(item);
    return equipment && equipped
      ? equipment.itemBudget - equipped.itemBudget
      : null;
  }

  handleInventoryItemClick(item: InventoryItem): void {
    if (this.isScrapMode) {
      this.toggleSelectItem(item);
      return;
    }

    this.selectInventoryItem(item);
    this.mobileItemInspectorOpen.set(true);
  }

  handleStockItemClick(item: InventoryItem): void {
    this.selectInventoryItem(item);
    this.mobileItemInspectorOpen.set(true);
  }

  closeItemInspector(): void {
    this.clearSelectedItem();
  }

  openItemDetails(item: InventoryItem): void {
    if (this.isEquipmentItem(item)) {
      this.modalService?.toggleInventoryEquipItemModal(
        item.itemInstance as EquipmentInstance,
      );
      return;
    }

    this.modalService?.toggleInventoryItemModal(item);
  }

  equipSlotOptions(item: InventoryItem): EquipmentSlotType[] {
    const equipment = this.equipmentInstance(item);
    return equipment
      ? getEquipSlotOptions(equipment.equipmentBase.equipmentType)
      : [];
  }

  equipActionLabel(item: InventoryItem, slotType: EquipmentSlotType): string {
    return this.equipmentInstance(item)?.equipmentBase.equipmentType ===
      EquipmentType.OneHanded
      ? `Equip ${this.equipmentSlotLabel(slotType).toLowerCase()}`
      : 'Equip';
  }

  equipItem(item: InventoryItem, slotType?: EquipmentSlotType): void {
    const equipment = this.equipmentInstance(item);
    if (!equipment || !this.equipmentState) return;

    this.equipmentState.equip(
      equipment,
      slotType ??
        getSlotTypeFromEquipmentType(equipment.equipmentBase.equipmentType),
    );
  }

  unequipItem(item: InventoryItem): void {
    const slotType = this.equippedSlotTypeFor(item);
    if (!slotType || !this.equipmentState || this.isEquipPending) return;

    this.equipmentState.unequip(slotType);
    this.clearEquipmentSlotFilter();
  }

  get isEquipPending(): boolean {
    return this.equipmentState?.loading() ?? false;
  }

  itemTypeLabel(item: InventoryItem): string {
    return (
      this.equipmentInstance(item)?.equipmentBase.equipmentType ??
      item.itemInstance.itemBase.itemType
    );
  }

  itemMetaLabel(item: InventoryItem): string {
    const equipment = this.equipmentInstance(item);
    if (equipment) {
      return `Tier ${equipment.tier} · ${equipment.quality}`;
    }

    return item.quantity === 1 ? '1 held' : `${item.quantity} held`;
  }

  itemRarityClass(item: InventoryItem): string {
    const rarity =
      this.equipmentInstance(item)?.rarity ?? item.itemInstance.itemBase.rarity;

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
        return 'text-secondary';
    }
  }

  private sortInventoryItems(
    items: InventoryItem[],
    sort: InventorySort | EquipmentInventorySort = this.inventorySort,
    direction: SortDirection = sort === 'Name' ? 'asc' : 'desc',
  ): InventoryItem[] {
    return [...items].sort((a, b) => {
      const aEquipment = this.equipmentInstance(a);
      const bEquipment = this.equipmentInstance(b);
      let difference = 0;

      switch (sort) {
        case 'Name':
          difference = this.itemDisplayName(a).localeCompare(
            this.itemDisplayName(b),
          );
          break;
        case 'Tier':
          difference = (aEquipment?.tier ?? 0) - (bEquipment?.tier ?? 0);
          break;
        case 'Rarity':
          difference =
            this.RARITY_ORDER[
              aEquipment?.rarity ?? a.itemInstance.itemBase.rarity
            ] -
            this.RARITY_ORDER[
              bEquipment?.rarity ?? b.itemInstance.itemBase.rarity
            ];
          break;
        case 'Quality':
          difference =
            (aEquipment ? this.QUALITY_ORDER[aEquipment.quality] : -1) -
            (bEquipment ? this.QUALITY_ORDER[bEquipment.quality] : -1);
          break;
        case 'Potential':
          difference =
            (aEquipment?.potential ?? 0) - (bEquipment?.potential ?? 0);
          break;
        case 'Gear Power':
          difference =
            (aEquipment?.itemBudget ?? 0) - (bEquipment?.itemBudget ?? 0);
          break;
      }

      return (
        (direction === 'asc' ? difference : -difference) ||
        this.itemDisplayName(a).localeCompare(this.itemDisplayName(b))
      );
    });
  }

  itemDisplayName(item: InventoryItem): string {
    return (
      this.equipmentInstance(item)?.displayName ??
      item.itemInstance.itemBase.name
    );
  }

  private clearSelectedItem(): void {
    this.selectedItem.set(null);
    this.mobileItemInspectorOpen.set(false);
    this.donationActionError.set(null);
    this.resetBlueprintAction();
    this.resetContainerAction();
  }

  private resetContainerAction(item?: InventoryItem): void {
    this.selectedContainerOptionId.set(
      item?.itemInstance.itemBase.selectionCrate?.options[0]?.id ?? '',
    );
    this.containerActionError.set(null);
  }

  private resetBlueprintAction(): void {
    this.selectedBlueprintRecipeId.set('');
    this.blueprintRecipeOptions.set([]);
    this.isLoadingBlueprintRecipes.set(false);
    this.hasLoadedBlueprintRecipes.set(false);
    this.isLearningBlueprint.set(false);
    this.blueprintActionError.set(null);
  }

  private loadBlueprintRecipes(item: InventoryItem): void {
    const blueprint = this.blueprintMetadata(item);
    if (!blueprint) {
      this.hasLoadedBlueprintRecipes.set(true);
      return;
    }

    if (!this.craftingService) {
      this.blueprintRecipeOptions.set(
        blueprint.compatibleRecipes.map((recipe) => ({
          label: recipe.name,
          value: recipe.id,
        })),
      );
      this.hasLoadedBlueprintRecipes.set(true);
      return;
    }

    const itemInstanceId = item.itemInstance.id;
    const compatibleRecipeIds = new Set(
      blueprint.compatibleRecipes.map((recipe) => recipe.id),
    );
    this.isLoadingBlueprintRecipes.set(true);
    this.craftingService.getRecipes().subscribe({
      next: (recipes) => {
        if (this.selectedItem()?.itemInstance.id !== itemInstanceId) return;

        const availableRecipeIds = new Set(
          recipes
            .filter(
              (recipe) =>
                compatibleRecipeIds.has(recipe.id) &&
                recipe.blueprints.some(
                  (candidate) =>
                    candidate.id === blueprint.blueprintId &&
                    !candidate.isLearned,
                ),
            )
            .map((recipe) => recipe.id),
        );
        this.blueprintRecipeOptions.set(
          blueprint.compatibleRecipes
            .filter((recipe) => availableRecipeIds.has(recipe.id))
            .map((recipe) => ({
              label: recipe.name,
              value: recipe.id,
            })),
        );
        this.isLoadingBlueprintRecipes.set(false);
        this.hasLoadedBlueprintRecipes.set(true);
      },
      error: (error) => {
        if (this.selectedItem()?.itemInstance.id !== itemInstanceId) return;

        this.blueprintActionError.set(
          error.message ?? 'Failed to load available recipes.',
        );
        this.isLoadingBlueprintRecipes.set(false);
        this.hasLoadedBlueprintRecipes.set(true);
      },
    });
  }

  private isBlueprintResource(item: InventoryItem): boolean {
    return isMarketplaceBlueprintResource(item.itemInstance.itemBase);
  }

  private isEntranceKeyResource(item: InventoryItem): boolean {
    const itemBase = item.itemInstance.itemBase;
    return (
      itemBase.itemType === ItemType.Resource &&
      itemBase.id.toLowerCase().startsWith('sigil_')
    );
  }

  private isCatalystResource(item: InventoryItem): boolean {
    const itemBase = item.itemInstance.itemBase;
    return (
      itemBase.itemType === ItemType.Resource &&
      MARKETPLACE_CATALYST_ITEM_IDS.has(itemBase.id.toLowerCase())
    );
  }

  private stockItemsForCategory(category: StockCategory): InventoryItem[] {
    switch (category) {
      case 'Resources':
        return this.state
          .materials()
          .filter(
            (item) =>
              !this.isBlueprintResource(item) &&
              !this.isEntranceKeyResource(item) &&
              !this.isCatalystResource(item),
          );
      case 'Blueprints':
        return this.state
          .materials()
          .filter((item) => this.isBlueprintResource(item));
      case 'Essences':
        return this.state.essences();
      case 'Entrance Keys':
        return this.state
          .materials()
          .filter((item) => this.isEntranceKeyResource(item));
      case 'Catalysts':
        return this.state
          .materials()
          .filter((item) => this.isCatalystResource(item));
    }
  }

  private inventoryItemForEquipment(
    equipment: EquipmentInstance,
  ): InventoryItem {
    return {
      id: equipment.id,
      itemInstance: equipment,
      quantity: 1,
      isFavorite: this.state.isFavorite(equipment.id),
    };
  }
}
