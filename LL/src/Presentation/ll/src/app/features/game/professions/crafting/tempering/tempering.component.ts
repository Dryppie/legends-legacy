import { NgClass, NgFor, NgIf, SlicePipe } from '@angular/common';
import {
  Component,
  computed,
  effect,
  Input,
  OnInit,
  signal,
  Signal,
} from '@angular/core';
import {
  CraftingQueueItem,
  CraftType,
} from '../../../../../shared/models/profession';
import {
  EquipmentInstance,
  ItemInstance,
} from '../../../../../shared/models/item';
import { InventoryDto } from '../../../../../shared/models/Dtos/inventoryDto';
import { AttributeTypeFormatPipe } from '../../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import {
  CharacterActionDto,
  StartCraftingActionRequest,
} from '../../../../../shared/models/Dtos/characterActionDto';
import { CraftingService } from '../../../../../core/services/api/crafting/crafting.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { CharacterActionType } from '../../../../../shared/models/enums/characterActionType';
import { CharacterActionsStateService } from '../../../../../core/services/api/character-actions/character-actions.state.service';

@Component({
  selector: 'app-tempering',
  standalone: true,
  imports: [NgFor, NgIf, NgClass, AttributeTypeFormatPipe, SlicePipe],
  templateUrl: './tempering.component.html',
})
export class TemperingComponent implements OnInit {
  @Input({ required: true }) inventory!: Signal<InventoryItem[]>;
  @Input({ required: true }) craftType!: CraftType;

  readonly craftingQueue;
  readonly isPerformingOtherAction = signal(false);

  private readonly selectedItemId = signal<string | null>(null);

  readonly selectedEquipmentInstance = computed<EquipmentInstance | null>(
    () => {
      const id = this.selectedItemId();
      const queue = this.craftingQueue();

      const invItem = this.inventoryState
        .items()
        .find((i) => i.itemInstance.id === id)?.itemInstance as
        | EquipmentInstance
        | undefined;

      return (
        invItem ??
        queue.find((q) => q.equipmentInstance.id === id)?.equipmentInstance ??
        null
      );
    },
  );
  readonly currentAction = signal<CharacterActionDto | null>(null); // declared safely first

  readonly canTemper = computed<boolean>(() => {
    const eq = this.selectedEquipmentInstance();
    return !!eq && (eq.potential ?? 0) > 0;
  });

  readonly isQueueSelected = computed<boolean>(() => {
    const id = this.selectedItemId();
    return (
      !!id && !this.inventoryState.items().some((i) => i.itemInstance.id === id)
    );
  });
  private checkTimeout: any = null;

  constructor(
    private readonly inventoryState: InventoryStateService,
    private readonly characterActionService: CharacterActionsStateService,
    private readonly craftingService: CraftingService,
  ) {
    this.craftingQueue = toSignal(this.craftingService.craftingQueue$, {
      initialValue: [] as CraftingQueueItem[],
    });

    effect(() => {
      const action = this.characterActionService.currentAction();

      if (!action) {
        this.clearCheckTimeout();
        queueMicrotask(() => this.isPerformingOtherAction.set(false));
        return;
      }

      const isCrafting =
        action.characterActionType === CharacterActionType.Crafting;

      if (
        isCrafting &&
        action.isDeleted &&
        action.craftingActionDetails?.craftingQueueItems.length
      ) {
        const queueCopy = [
          ...(action.craftingActionDetails?.craftingQueueItems ?? []),
        ];
        queueMicrotask(() => this.cancelEntireQueue(queueCopy));
      }

      if (isCrafting) {
        this.clearCheckTimeout();
        queueMicrotask(() => this.isPerformingOtherAction.set(false));
        return;
      }

      const updatedAt = new Date(action.updatedAt ?? 0).getTime();
      const now = Date.now();

      if (action.isDeleted && updatedAt > now) {
        this.clearCheckTimeout();
        this.checkTimeout = setTimeout(() => {
          queueMicrotask(() => this.isPerformingOtherAction.set(false));
        }, updatedAt - now);
      } else {
        this.clearCheckTimeout();
      }

      queueMicrotask(() => this.isPerformingOtherAction.set(false));
    });
  }

  ngOnInit(): void {}

  ngOnDestroy(): void {
    this.clearCheckTimeout();
  }

  private clearCheckTimeout(): void {
    if (this.checkTimeout) {
      clearTimeout(this.checkTimeout);
      this.checkTimeout = null;
    }
  }

  handleEquipmentInstanceAndTempering(
    inventory: InventoryDto,
    id: string | null,
    queue: CraftingQueueItem[],
  ): EquipmentInstance {
    const inventoryItem =
      (inventory.inventoryItems.find((i) => i.itemInstance.id === id)
        ?.itemInstance as EquipmentInstance) ?? null;
    let equipment =
      inventoryItem ??
      queue.find((q) => q.equipmentInstance.id === id)?.equipmentInstance ??
      null;

    return equipment;
  }

  selectItem(e: ItemInstance): void {
    this.selectedItemId.set(e.id);
  }

  temper(equipment: EquipmentInstance): void {
    if (!equipment) return;
    // take the latest inventory once, synchronously
    const items = this.inventoryState.items();
    if (!items) return;

    if (!equipment.potential || equipment.potential <= 0) {
      return;
    }

    const queueItem: CraftingQueueItem = {
      id: crypto.randomUUID(),
      equipmentInstance: equipment,
    };

    const startCraftingActionRequest: StartCraftingActionRequest = {
      queueId: queueItem.id,
      itemInstanceId: equipment.id,
    };
    this.characterActionService.startAction(
      CharacterActionType.Crafting,
      startCraftingActionRequest,
    );

    this.selectedItemId.set(null);
    this.craftingService.enqueueTempering(queueItem);
    this.inventoryState.removeItem(equipment.id);
  }

  cancelCraft(queueItem: CraftingQueueItem): void {
    if (!queueItem) return;

    const items = this.inventoryState.items();
    if (!items) return;
    this.craftingService.removeItemFromQueue(queueItem).subscribe((success) => {
      /* TODO: might be necessary, but only if removing items from queue is deemed troublesome in the backend, causing client-side miss-match */
    });
    this.inventoryState.addOrIncrement({
      id: items[0].id,
      quantity: 1,
      itemInstance: queueItem.equipmentInstance,
    });
    this.craftingService.dequeueTempering(queueItem.id);
    if (this.craftingService.currentQueue.length === 0) {
      this.clearCurrentAction();
    }

    this.selectedItemId.set(null);
  }

  cancelEntireQueue(queue: CraftingQueueItem[]) {
    if (!queue.length) return;

    const items = this.inventoryState.items();
    if (!items) return;

    queue.forEach((queueItem) => {
      this.inventoryState.addOrIncrement({
        id: items[0].id,
        quantity: 1,
        itemInstance: queueItem.equipmentInstance,
      });
      this.craftingService.dequeueTempering(queueItem.id);
    });
    this.clearCurrentAction();
    this.selectedItemId.set(null);
  }

  clearCurrentAction() {
    let action = this.characterActionService.currentAction();
    action!.craftingActionDetails!.craftingQueueItems = [];
    this.characterActionService.currentAction.set(action);
    this.characterActionService.clear();
  }

  getEstimatedTime(queue: CraftingQueueItem[]): string {
    const totalSeconds = queue.reduce((sum, q) => {
      const potential = q.equipmentInstance?.potential ?? 0;
      return sum + potential * 6;
    }, 0);

    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    const parts = [];
    if (hours > 0) parts.push(`${hours}h`);
    if (minutes > 0) parts.push(`${minutes}m`);
    if (seconds > 0 || parts.length === 0) parts.push(`${seconds}s`);

    return parts.join(' ');
  }
}
