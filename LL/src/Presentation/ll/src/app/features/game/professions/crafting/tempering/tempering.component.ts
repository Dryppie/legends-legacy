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
import { CharacterManagerService } from '../../../../../core/services/client-side/character-manager/character-manager.service';

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

  constructor(private readonly characterManager: CharacterManagerService) {}

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
      equipment: equipment,
      startedAt: new Date(),
      status: CraftingQueueStatus.Queued,
    };

    /* optimistic queue */
    this.craftingQueue$.next([...this.craftingQueue$.value, queueItem]);

    /* optimistic client-side material removal */
    const updatedItems = items.filter(
      (i) => i.itemInstance.id !== equipment.id,
    );
    this.characterManager.setInventory({ inventoryItems: updatedItems });
    this.selectedItemId$.next(null);
    // this.craftingService.craftItem(recipe.id);
  }

  cancelCraft(queueItem: CraftingQueueItem): void {
    if (!queueItem) return;

    const inventory = this.characterManager.getInventory();
    if (!inventory) return;

    const items = inventory.inventoryItems;
    let inventoryItem: InventoryItem = {
      id: crypto.randomUUID(),
      quantity: 1,
      itemInstance: queueItem.equipment,
    };
    items.push(inventoryItem);
    this.characterManager.setInventory({ inventoryItems: items });

    // TODO: cancel via service
    this.craftingQueue$.next(
      this.craftingQueue$.value.filter((r) => r.id !== queueItem.id),
    );
  }
}
