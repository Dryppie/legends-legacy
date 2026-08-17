import { DatePipe, DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import {
  Component,
  computed,
  effect,
  Input,
  OnDestroy,
  signal,
  Signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CraftingQueueItem } from '../../../../../shared/models/profession';
import {
  Equipment,
  EquipmentInstance,
  ItemInstance,
} from '../../../../../shared/models/item';
import {
  CraftingQueueMoveDirection,
  CraftingService,
} from '../../../../../core/services/api/crafting/crafting.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { ItemComponent } from '../../../../../shared/components/item/item.component';
import { EquipmentDisplayComponent } from '../../../../../shared/components/equipment/equipment-display/equipment-display.component';
import { EquipmentType } from '../../../../../shared/models/enums/equipmentType';
import { CharacterActionsStateService } from '../../../../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../../../../shared/models/enums/characterActionType';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import { Rarity } from '../../../../../shared/models/enums/rarity';
import {
  TemperingOutcome,
  TemperingOutcomeEntry,
} from '../../../../../shared/models/Dtos/temperingSessionDto';
import { formatAttributeType } from '../../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { formatAttributeValue } from '../../../../../shared/pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import { getEstimatedTemperingQueueDuration } from '../../../../../shared/utils/tempering/tempering-duration.utils';
import { ItemQuality } from '../../../../../shared/models/enums/itemQuality';

type TemperingSort = 'Name' | 'Quality' | 'Potential' | 'Gear Power';
type SortDirection = 'asc' | 'desc';

const QUALITY_ORDER: Record<ItemQuality, number> = {
  [ItemQuality.Crude]: 0,
  [ItemQuality.Standard]: 1,
  [ItemQuality.Fine]: 2,
  [ItemQuality.Exceptional]: 3,
  [ItemQuality.Masterwork]: 4,
};

@Component({
  selector: 'app-tempering',
  imports: [
    DatePipe,
    DecimalPipe,
    NgFor,
    NgIf,
    NgClass,
    ItemComponent,
    EquipmentDisplayComponent,
  ],
  templateUrl: './tempering.component.html',
})
export class TemperingComponent implements OnDestroy {
  @Input({ required: true }) inventory!: Signal<InventoryItem[]>;

  private readonly itemXpPerRarity = 10;

  readonly craftingQueue: Signal<CraftingQueueItem[]>;
  readonly recentOutcomes: Signal<TemperingOutcomeEntry[]>;
  readonly outcomesOpen = signal(false);
  readonly error = signal<string | null>(null);
  readonly removingQueueItemId = signal<string | null>(null);
  readonly movingQueueItemId = signal<string | null>(null);
  readonly temperingSort = signal<TemperingSort>('Gear Power');
  readonly sortDirection = signal<SortDirection>('desc');
  readonly activeQueueItem = computed<CraftingQueueItem | null>(
    () => this.craftingQueue()[0] ?? null,
  );
  readonly waitingQueue = computed<CraftingQueueItem[]>(() =>
    this.craftingQueue().slice(1),
  );
  readonly actionUnavailable = computed(
    () =>
      this.characterActionsState.loadingCombat() ||
      !this.characterActionsState.canStartAction(CharacterActionType.Crafting),
  );

  readonly equipmentInventory = computed(() =>
    this.inventory().filter((inventoryItem) =>
      this.isNonToolEquipment(inventoryItem.itemInstance),
    ),
  );

  readonly filteredInventory = computed(() => {
    const eligibleItems = this.equipmentInventory().filter((inventoryItem) => {
      const equipment = inventoryItem.itemInstance as EquipmentInstance;
      return (
        (equipment.potential ?? 0) > 0 &&
        equipment.rarity !== Rarity.Legacy &&
        !!equipment.baseRecipeId
      );
    });

    return this.sortInventory(
      eligibleItems,
      this.temperingSort(),
      this.sortDirection(),
    );
  });

  readonly unavailableEquipmentCount = computed(
    () => this.equipmentInventory().length - this.filteredInventory().length,
  );

  private readonly selectedItemId = signal<string | null>(null);

  readonly selectedQueueItem = computed<CraftingQueueItem | null>(() => {
    const id = this.selectedItemId();
    return id
      ? (this.craftingQueue().find(
          (item) => item.equipmentInstance.id === id,
        ) ?? null)
      : null;
  });

  readonly selectedEquipmentInstance = computed<EquipmentInstance | null>(
    () => {
      const id = this.selectedItemId();
      return (
        (this.inventoryState.items().find((i) => i.itemInstance.id === id)
          ?.itemInstance as EquipmentInstance | undefined) ??
        this.selectedQueueItem()?.equipmentInstance ??
        null
      );
    },
  );

  readonly canTemper = computed<boolean>(() => {
    const eq = this.selectedEquipmentInstance();
    return (
      !!eq &&
      !this.actionUnavailable() &&
      this.isNonToolEquipment(eq) &&
      (eq.potential ?? 0) >= 1 &&
      eq.rarity !== Rarity.Legacy &&
      !!eq.baseRecipeId
    );
  });

  readonly selectedIneligibilityReason = computed<string | null>(() => {
    const equipment = this.selectedEquipmentInstance();
    if (!equipment || this.canTemper()) return null;
    if (this.characterActionsState.isActionCooldown())
      return 'Combat is stopping. Tempering will be available when the current action timer finishes.';
    if (this.actionUnavailable())
      return 'Tempering cannot be started while combat is in progress.';
    if ((equipment.potential ?? 0) < 1)
      return 'This item has no remaining Potential.';
    if (equipment.rarity === Rarity.Legacy)
      return 'Legacy items cannot be tempered further.';
    if (!equipment.baseRecipeId)
      return 'This legacy item is not connected to a current base recipe.';
    return 'This item cannot be tempered.';
  });

  constructor(
    private readonly inventoryState: InventoryStateService,
    private readonly craftingService: CraftingService,
    private readonly characterActionsState: CharacterActionsStateService,
  ) {
    this.craftingService.clearTemperingOutcomes();
    this.recentOutcomes = this.craftingService.recentTemperingOutcomes;
    this.craftingQueue = toSignal(this.craftingService.craftingQueue$, {
      initialValue: [] as CraftingQueueItem[],
    });

    effect(
      () => {
        const selectedId = this.selectedItemId();
        if (selectedId) return;

        const active = this.activeQueueItem();
        if (active) {
          this.selectedItemId.set(active.equipmentInstance.id);
        }
      },
      { allowSignalWrites: true },
    );
  }

  selectItem(e: ItemInstance): void {
    this.selectedItemId.set(e.id);
  }

  equipmentInstance(item: InventoryItem): EquipmentInstance {
    return item.itemInstance as EquipmentInstance;
  }

  setTemperingSort(sort: TemperingSort): void {
    if (this.temperingSort() === sort) {
      this.sortDirection.update((direction) =>
        direction === 'asc' ? 'desc' : 'asc',
      );
      return;
    }

    this.temperingSort.set(sort);
    this.sortDirection.set(sort === 'Name' ? 'asc' : 'desc');
  }

  sortIndicator(sort: TemperingSort): string {
    if (this.temperingSort() !== sort) return '';
    return this.sortDirection() === 'asc' ? '↑' : '↓';
  }

  ariaSort(sort: TemperingSort): 'ascending' | 'descending' | 'none' {
    if (this.temperingSort() !== sort) return 'none';
    return this.sortDirection() === 'asc' ? 'ascending' : 'descending';
  }

  ngOnDestroy(): void {
    this.craftingService.clearTemperingOutcomes();
  }

  temper(equipment: EquipmentInstance): void {
    if (!equipment || this.actionUnavailable() || !this.canTemper()) return;

    const queueId = crypto.randomUUID();
    const queueItem: CraftingQueueItem = {
      id: queueId,
      equipmentInstance: equipment,
    };

    this.characterActionsState.startAction(CharacterActionType.Crafting, {
      queueId,
      itemInstanceId: equipment.id,
    });
    this.craftingService.setQueue([
      ...this.craftingService.currentQueue,
      queueItem,
    ]);

    this.inventoryState.setInventory(
      this.inventoryState
        .items()
        .filter((item) => item.itemInstance.id !== equipment.id),
    );
    this.selectedItemId.set(equipment.id);
  }

  selectQueuedItem(queueItem: CraftingQueueItem): void {
    this.selectedItemId.set(queueItem.equipmentInstance.id);
  }

  getEstimatedTime(queue: CraftingQueueItem[]): string {
    return getEstimatedTemperingQueueDuration(queue);
  }

  itemXpPercent(equipment: EquipmentInstance): number {
    return Math.min(
      Math.max(((equipment.itemXp ?? 0) / this.itemXpPerRarity) * 100, 0),
      100,
    );
  }

  itemXpShortLabel(equipment: EquipmentInstance): string {
    const currentXp = Math.min(
      Math.max(equipment.itemXp ?? 0, 0),
      this.itemXpPerRarity,
    );
    return `${currentXp} / ${this.itemXpPerRarity} EXP`;
  }

  private isNonToolEquipment(item: ItemInstance): item is EquipmentInstance {
    const equipment = item as EquipmentInstance;
    const equipmentType =
      equipment.equipmentBase?.equipmentType ??
      (equipment.itemBase as Equipment | undefined)?.equipmentType;
    const isEquipment =
      item.itemBase?.itemType === ItemType.Equipment ||
      equipment.equipmentBase != null;

    return isEquipment && equipmentType !== EquipmentType.Tool;
  }

  removeQueuedItem(queueItem: CraftingQueueItem, event: MouseEvent): void {
    event.stopPropagation();
    if (this.removingQueueItemId()) return;

    this.removingQueueItemId.set(queueItem.id);
    this.error.set(null);
    const wasSelected = this.selectedQueueItem()?.id === queueItem.id;

    this.craftingService.removeItemFromQueue(queueItem).subscribe({
      next: (response) => {
        const nextQueue =
          response.currentAction?.craftingActionDetails?.craftingQueueItems ??
          [];
        this.inventoryState.setInventory(response.inventoryItems);
        this.craftingService.setQueue(nextQueue);
        this.characterActionsState.refreshCurrentAction();

        if (wasSelected) {
          this.selectedItemId.set(queueItem.equipmentInstance.id);
        }

        this.removingQueueItemId.set(null);
      },
      error: (err) => {
        this.error.set(err.message ?? 'Failed to remove item from queue.');
        this.removingQueueItemId.set(null);
      },
    });
  }

  outcomeLabel(outcome: TemperingOutcome): string {
    return outcome === 'Critical' ? 'Critical success' : outcome;
  }

  toggleOutcomes(): void {
    this.outcomesOpen.update((open) => !open);
  }

  outcomeDetail(entry: TemperingOutcomeEntry): string {
    if (entry.becameLevelingItem) return 'Awakened as a Leveling Item';
    if (entry.becameMasterpiece) return 'Became a Masterpiece';
    if (entry.qualityIncreased && entry.previousQuality && entry.newQuality) {
      return `Quality increased: ${entry.previousQuality} → ${entry.newQuality}`;
    }
    if (entry.rarityUpgraded) {
      const improvement = this.statImprovement(entry);
      return `Rarity increased: ${entry.previousRarity} → ${entry.newRarity}${
        improvement ? ` · ${improvement}` : ''
      }`;
    }
    if (entry.outcome === 'Positive') {
      return `Tempering EXP: ${entry.previousItemXp} → ${entry.newItemXp}`;
    }
    if (entry.outcome === 'Negative') {
      const extraPotentialLost = Math.max(
        0,
        entry.previousPotential - entry.newPotential - entry.potentialSpent,
      );
      if (extraPotentialLost > 0) {
        return `Lost ${extraPotentialLost} additional Potential`;
      }
      const itemXpLost = Math.max(0, entry.previousItemXp - entry.newItemXp);
      return itemXpLost > 0
        ? `Lost ${itemXpLost} Tempering EXP`
        : 'No additional penalty';
    }
    if (entry.outcome === 'Critical') return 'No further quality increase';
    return 'No improvement this attempt';
  }

  outcomeCardClasses(outcome: TemperingOutcome): Record<string, boolean> {
    return {
      'border-amber-300/50 bg-amber-300/10': outcome === 'Critical',
      'border-emerald-400/40 bg-emerald-400/10': outcome === 'Positive',
      'border-rose-400/40 bg-rose-400/10': outcome === 'Negative',
      'border-white/15 bg-white/5': outcome === 'Neutral',
    };
  }

  outcomeLabelClasses(outcome: TemperingOutcome): Record<string, boolean> {
    return {
      'text-amber-200': outcome === 'Critical',
      'text-emerald-300': outcome === 'Positive',
      'text-rose-300': outcome === 'Negative',
      'text-secondary': outcome === 'Neutral',
    };
  }

  trackOutcome(_: number, outcome: TemperingOutcomeEntry): string {
    return outcome.id;
  }

  private statImprovement(entry: TemperingOutcomeEntry): string | null {
    if (
      !entry.improvedStat ||
      entry.previousStatValue == null ||
      entry.newStatValue == null
    ) {
      return null;
    }

    const increase = entry.newStatValue - entry.previousStatValue;
    return `${formatAttributeType(entry.improvedStat)} ${formatAttributeValue(
      increase,
      entry.improvedStat,
      true,
    )}`;
  }

  moveQueuedItem(
    queueItem: CraftingQueueItem,
    direction: CraftingQueueMoveDirection,
    event: MouseEvent,
  ): void {
    event.stopPropagation();
    if (this.movingQueueItemId() || this.removingQueueItemId()) return;

    this.movingQueueItemId.set(queueItem.id);
    this.error.set(null);

    this.craftingService.moveQueueItem(queueItem.id, direction).subscribe({
      next: (response) => {
        const queue =
          response.currentAction.craftingActionDetails?.craftingQueueItems ??
          [];
        this.craftingService.setQueue(queue);
        this.characterActionsState.applyCurrentActionSnapshot(
          response.currentAction,
        );
        this.movingQueueItemId.set(null);
      },
      error: (err) => {
        this.error.set(err.message ?? 'Failed to reposition the queue item.');
        this.movingQueueItemId.set(null);
      },
    });
  }

  queueIsBusy(): boolean {
    return !!this.movingQueueItemId() || !!this.removingQueueItemId();
  }

  private sortInventory(
    items: InventoryItem[],
    sort: TemperingSort,
    direction: SortDirection,
  ): InventoryItem[] {
    return [...items].sort((left, right) => {
      const leftEquipment = this.equipmentInstance(left);
      const rightEquipment = this.equipmentInstance(right);
      let difference = 0;

      switch (sort) {
        case 'Name':
          difference = this.itemDisplayName(leftEquipment).localeCompare(
            this.itemDisplayName(rightEquipment),
          );
          break;
        case 'Quality':
          difference =
            QUALITY_ORDER[leftEquipment.quality] -
            QUALITY_ORDER[rightEquipment.quality];
          break;
        case 'Potential':
          difference =
            (leftEquipment.potential ?? 0) - (rightEquipment.potential ?? 0);
          break;
        case 'Gear Power':
          difference = leftEquipment.itemBudget - rightEquipment.itemBudget;
          break;
      }

      return (
        (direction === 'asc' ? difference : -difference) ||
        this.itemDisplayName(leftEquipment).localeCompare(
          this.itemDisplayName(rightEquipment),
        )
      );
    });
  }

  private itemDisplayName(equipment: EquipmentInstance): string {
    return equipment.displayName || equipment.itemBase.name;
  }
}
