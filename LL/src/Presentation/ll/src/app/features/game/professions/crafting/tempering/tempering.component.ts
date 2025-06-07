import { NgClass, NgFor, NgIf, SlicePipe } from '@angular/common';
import {
  Component,
  computed,
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
import { CharacterManagerService } from '../../../../../core/services/client-side/character-manager/character-manager.service';
import { CharacterActionsService } from '../../../../../core/services/api/character-actions/character-actions.service';
import { StartCraftingActionRequest } from '../../../../../shared/models/Dtos/characterActionDto';
import { CraftingService } from '../../../../../core/services/api/crafting/crafting.service';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-tempering',
  standalone: true,
  imports: [NgFor, NgIf, NgClass, AttributeTypeFormatPipe, SlicePipe],
  templateUrl: './tempering.component.html',
})
export class TemperingComponent implements OnInit {
  @Input({ required: true }) inventory!: Signal<InventoryDto | null>;
  @Input({ required: true }) craftType!: CraftType;

  readonly craftingQueue;

  private readonly selectedItemId = signal<string | null>(null);

  readonly selectedEquipmentInstance = computed<EquipmentInstance | null>(
    () => {
      const id = this.selectedItemId();
      const inv = this.inventory();
      const queue = this.craftingQueue();

      const invItem = inv?.inventoryItems.find((i) => i.itemInstance.id === id)
        ?.itemInstance as EquipmentInstance | undefined;

      return (
        invItem ??
        queue.find((q) => q.equipmentInstance.id === id)?.equipmentInstance ??
        null
      );
    },
  );

  readonly canTemper = computed<boolean>(() => {
    const eq = this.selectedEquipmentInstance();
    return !!eq && (eq.potential ?? 0) > 0;
  });

  readonly isQueueSelected = computed<boolean>(() => {
    const id = this.selectedItemId();
    const inv = this.inventory();
    return !!id && !inv?.inventoryItems.some((i) => i.itemInstance.id === id);
  });

  constructor(
    private readonly characterManager: CharacterManagerService,
    private readonly characterActionService: CharacterActionsService,
    private readonly craftingService: CraftingService,
  ) {
    this.craftingQueue = toSignal(this.craftingService.craftingQueue$, {
      initialValue: [] as CraftingQueueItem[],
    });
  }

  ngOnInit(): void {}

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
    const inventory = this.characterManager.getInventory();
    if (!inventory) return;
    const items = inventory.inventoryItems;
    if (!equipment.potential || equipment.potential <= 0) {
      return;
    }

    const queueItem: CraftingQueueItem = {
      id: crypto.randomUUID(),
      equipmentInstance: equipment,
    };
    /* optimistic queue */

    /* optimistic client-side material removal */
    const updatedItems = items.filter(
      (i) => i.itemInstance.id !== equipment.id,
    );
    const startCraftingActionRequest: StartCraftingActionRequest = {
      queueId: queueItem.id,
      itemInstanceId: equipment.id,
    };
    this.characterActionService
      .startCraftingAction(startCraftingActionRequest)
      .subscribe((success) => {
        if (success) {
          this.selectedItemId.set(null);
          this.craftingService.enqueueTempering(queueItem);
          this.characterManager.setInventory({ inventoryItems: updatedItems });
        }
      });
  }

  cancelCraft(queueItem: CraftingQueueItem): void {
    if (!queueItem) return;

    const inventory = this.characterManager.getInventory();
    if (!inventory) return;

    inventory.inventoryItems.push({
      id: inventory.inventoryItems[0].id,
      quantity: 1,
      itemInstance: queueItem.equipmentInstance,
    });

    this.craftingService.removeItemFromQueue(queueItem).subscribe((success) => {
      /* TODO: might be necessary, but only if removing items from queue is deemed troublesome in the backend, causing client-side miss-match */
    });
    this.characterManager.setInventory(inventory);
    this.craftingService.dequeueTempering(queueItem.id);
    if (this.craftingService.currentQueue.length === 0)
      this.characterActionService.clearCurrentAction();
  }
}
