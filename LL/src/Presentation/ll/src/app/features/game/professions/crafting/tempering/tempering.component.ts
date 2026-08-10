import { NgClass, NgFor, NgIf } from '@angular/common';
import {
  Component,
  computed,
  effect,
  Input,
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
import { CraftingService } from '../../../../../core/services/api/crafting/crafting.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { ItemComponent } from '../../../../../shared/components/item/item.component';
import { EquipmentDisplayComponent } from '../../../../../shared/components/equipment/equipment-display/equipment-display.component';
import { EquipmentType } from '../../../../../shared/models/enums/equipmentType';
import { CharacterActionsStateService } from '../../../../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../../../../shared/models/enums/characterActionType';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import { Rarity } from '../../../../../shared/models/enums/rarity';

@Component({
    selector: 'app-tempering',
    imports: [NgFor, NgIf, NgClass, ItemComponent, EquipmentDisplayComponent],
    templateUrl: './tempering.component.html'
})
export class TemperingComponent {
  @Input({ required: true }) inventory!: Signal<InventoryItem[]>;

  private readonly itemXpPerRarity = 10;
  private readonly temperingActionDurationSeconds = 10;

  readonly craftingQueue: Signal<CraftingQueueItem[]>;
  readonly error = signal<string | null>(null);
  readonly lastOutcome = signal<string | null>(null);
  readonly removingQueueItemId = signal<string | null>(null);
  readonly activeQueueItem = computed<CraftingQueueItem | null>(
    () => this.craftingQueue()[0] ?? null,
  );
  readonly waitingQueue = computed<CraftingQueueItem[]>(() =>
    this.craftingQueue().slice(1),
  );
  readonly combatInProgress = computed(
    () =>
      this.characterActionsState.loadingCombat() ||
      (this.characterActionsState.isCombatAction() &&
        this.characterActionsState.isActiveAction()),
  );

  readonly equipmentInventory = computed(() =>
    this.inventory().filter((inventoryItem) =>
      this.isNonToolEquipment(inventoryItem.itemInstance),
    ),
  );

  readonly filteredInventory = computed(() =>
    this.equipmentInventory().filter((inventoryItem) => {
      const equipment = inventoryItem.itemInstance as EquipmentInstance;
      return (
        (equipment.potential ?? 0) > 0 &&
        equipment.rarity !== Rarity.Legacy &&
        !!equipment.baseRecipeId
      );
    }),
  );

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

  readonly selectedQueueStatus = computed<string | null>(() => {
    const queueItem = this.selectedQueueItem();
    if (!queueItem) return null;

    const queueIndex = this.craftingQueue().findIndex(
      (item) => item.id === queueItem.id,
    );

    if (queueIndex === 0) {
      return 'Working on this item now.';
    }

    if (queueIndex === 1) {
      return 'Queued next. The current working item will finish first.';
    }

    return `Queued position ${queueIndex + 1}. Current and earlier queued items will finish first.`;
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
      !this.combatInProgress() &&
      this.isNonToolEquipment(eq) &&
      (eq.potential ?? 0) >= 1 &&
      eq.rarity !== Rarity.Legacy &&
      !!eq.baseRecipeId
    );
  });

  readonly selectedIneligibilityReason = computed<string | null>(() => {
    const equipment = this.selectedEquipmentInstance();
    if (!equipment || this.canTemper()) return null;
    if (this.combatInProgress())
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
    this.lastOutcome.set(null);
  }

  temper(equipment: EquipmentInstance): void {
    if (!equipment || this.combatInProgress() || !this.canTemper()) return;

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
    this.lastOutcome.set(
      `Queued ${equipment.displayName ?? equipment.itemBase.name} for tempering`,
    );
  }

  selectQueuedItem(queueItem: CraftingQueueItem): void {
    this.selectedItemId.set(queueItem.equipmentInstance.id);
    this.lastOutcome.set(null);
  }

  getEstimatedTime(queue: CraftingQueueItem[]): string {
    const totalSeconds = queue.reduce((sum, item) => {
      return (
        sum +
        Math.max(0, item.equipmentInstance.potential ?? 0) *
          this.temperingActionDurationSeconds
      );
    }, 0);

    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    const parts: string[] = [];
    if (hours > 0) parts.push(`${hours}h`);
    if (minutes > 0) parts.push(`${minutes}m`);
    if (seconds > 0 || parts.length === 0) parts.push(`${seconds}s`);

    return parts.join(' ');
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

        this.lastOutcome.set(
          `Removed ${queueItem.equipmentInstance.displayName ?? queueItem.equipmentInstance.itemBase.name} from the tempering queue`,
        );
        this.removingQueueItemId.set(null);
      },
      error: (err) => {
        this.error.set(err.message ?? 'Failed to remove item from queue.');
        this.removingQueueItemId.set(null);
      },
    });
  }
}
