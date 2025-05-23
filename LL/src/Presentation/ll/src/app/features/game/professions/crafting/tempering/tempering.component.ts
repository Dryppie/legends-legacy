import { AsyncPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import {
  CraftingQueueItem,
  CraftingQueueStatus,
  CraftType,
} from '../../../../../shared/models/profession';
import {
  BehaviorSubject,
  combineLatest,
  map,
  Observable,
  ReplaySubject,
} from 'rxjs';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { EquipmentInstance } from '../../../../../shared/models/item';
import { InventoryDto } from '../../../../../shared/models/Dtos/inventoryDto';
import { AttributeTypeFormatPipe } from '../../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';

@Component({
  selector: 'app-tempering',
  standalone: true,
  imports: [NgFor, NgIf, NgClass, AsyncPipe, AttributeTypeFormatPipe],
  templateUrl: './tempering.component.html',
  styleUrl: './tempering.component.css',
})
export class TemperingComponent implements OnInit {
  @Input() inventory$!: Observable<InventoryDto>;
  @Input() craftType!: CraftType;

  readonly craftingQueue$ = new BehaviorSubject<CraftingQueueItem[]>([]);
  private readonly selectedItemId$ = new BehaviorSubject<string | null>(null);
  readonly selectedEquipmentInstance$ =
    new ReplaySubject<EquipmentInstance | null>(1);

  ngOnInit(): void {
    combineLatest([this.inventory$, this.selectedItemId$])
      .pipe(
        map(
          ([inventory, id]) =>
            (inventory.inventoryItems.find((i) => i.itemInstance.id === id)
              ?.itemInstance as EquipmentInstance) ?? null,
        ),
      )
      .subscribe(this.selectedEquipmentInstance$);
  }
  selectItem(item: InventoryItem): void {
    this.selectedItemId$.next(item.itemInstance.id);
  }

  temper(inventoryItem: InventoryItem): void {
    const equipment = inventoryItem.itemInstance as EquipmentInstance;
    if (!equipment) return;
    // take the latest inventory once, synchronously
    // const inventory = this.characterManager.getInventory();
    // if (!inventory) return;
    // const items = inventory.inventoryItems;
    // if (
    //   !recipe.materials.every((m) => hasQuantity(items, m.item.id, m.quantity))
    // ) {
    //   return; // safety net – shouldn’t happen if button was disabled
    // }

    const queueItem: CraftingQueueItem = {
      id: crypto.randomUUID(),
      equipment: equipment,
      startedAt: new Date(),
      status: CraftingQueueStatus.Queued,
    };

    /* optimistic queue */
    this.craftingQueue$.next([...this.craftingQueue$.value, queueItem]);

    /* optimistic client-side material removal */
    // const updatedItems = consumeMaterials(items, recipe);
    // this.characterManager.setInventory({ inventoryItems: updatedItems });

    // this.craftingService.craftItem(recipe.id);
  }

  cancelCraft(queueItem: CraftingQueueItem): void {
    // TODO: cancel via service
    this.craftingQueue$.next(
      this.craftingQueue$.value.filter((r) => r.id !== queueItem.id),
    );
  }
}
