import { AsyncPipe, NgClass, NgFor, NgIf, SlicePipe } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import {
  CraftingQueueItem,
  CraftType,
} from '../../../../../shared/models/profession';
import {
  BehaviorSubject,
  combineLatest,
  map,
  Observable,
  ReplaySubject,
  tap,
} from 'rxjs';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
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

@Component({
  selector: 'app-tempering',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    NgClass,
    AsyncPipe,
    AttributeTypeFormatPipe,
    SlicePipe,
  ],
  templateUrl: './tempering.component.html',
  styleUrl: './tempering.component.css',
})
export class TemperingComponent implements OnInit {
  @Input() inventory$!: Observable<InventoryDto>;
  @Input() craftType!: CraftType;
  canTemper = false;
  isQueueSelected = false;
  readonly craftingQueue$ = new Observable<CraftingQueueItem[]>();
  private readonly selectedItemId$ = new BehaviorSubject<string | null>(null);
  readonly selectedEquipmentInstance$ =
    new ReplaySubject<EquipmentInstance | null>(1);

  constructor(
    private readonly characterManager: CharacterManagerService,
    private readonly characterActionService: CharacterActionsService,
    private readonly craftingService: CraftingService,
  ) {
    this.craftingQueue$ = craftingService.craftingQueue$;
  }

  ngOnInit(): void {
    combineLatest([this.inventory$, this.selectedItemId$, this.craftingQueue$])
      .pipe(
        map(([inventory, id, queue]) =>
          this.handleEquipmentInstanceAndTempering(inventory, id, queue),
        ),
      )
      .subscribe(this.selectedEquipmentInstance$);
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
    if (equipment?.potential && equipment.potential > 0) this.canTemper = true;
    else this.canTemper = false;
    this.isQueueSelected = equipment && !inventoryItem;

    return equipment;
  }

  selectItem(equipment: ItemInstance): void {
    this.selectedItemId$.next(equipment.id);
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
          this.selectedItemId$.next(null);
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
      /* might be necessary if removing items from queue is deemed troublesome*/
    });
    this.characterManager.setInventory(inventory);
    this.craftingService.dequeueTempering(queueItem.id);
    if (this.craftingService.currentQueue.length === 0)
      this.characterActionService.clearCurrentAction();
  }
}
