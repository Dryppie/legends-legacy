import { AsyncPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import {
  CraftingQueueItem,
  CraftingQueueStatus,
  CraftType,
  Profession,
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
  Equipment,
  EquipmentInstance,
} from '../../../../../shared/models/item';
import { InventoryDto } from '../../../../../shared/models/Dtos/inventoryDto';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import { EquipmentType } from '../../../../../shared/models/Dtos/equipmentSlot';

@Component({
  selector: 'app-tempering',
  standalone: true,
  imports: [NgFor, NgIf, NgClass, AsyncPipe],
  templateUrl: './tempering.component.html',
  styleUrl: './tempering.component.css',
})
export class TemperingComponent implements OnInit {
  @Input() inventory$!: Observable<InventoryDto>;
  @Input() craftType!: CraftType;

  allowedTypesByCraft: Record<CraftType, EquipmentType[]> = {
    [CraftType.JewelryCrafting]: [EquipmentType.Ring, EquipmentType.Necklace],
    [CraftType.ArmorForging]: [
      EquipmentType.Head,
      EquipmentType.Chest,
      EquipmentType.Legs,
      EquipmentType.Relic,
    ],
    [CraftType.WeaponSmithing]: [EquipmentType.MainHand, EquipmentType.OffHand],
  };

  filteredInventory$!: Observable<InventoryItem[]>;

  readonly craftingQueue$ = new BehaviorSubject<CraftingQueueItem[]>([]);
  private readonly selectedItemId$ = new BehaviorSubject<string | null>(null);
  readonly selectedItem$ = new ReplaySubject<InventoryItem | null>(1);

  ngOnInit(): void {
    this.filteredInventory$ = this.inventory$.pipe(
      map((inventory) => {
        const allowedTypes = this.allowedTypesByCraft[this.craftType];
        return inventory.inventoryItems.filter(
          (i) =>
            i.itemInstance.itemBase.itemType === ItemType.Equipment &&
            allowedTypes.includes(
              (i.itemInstance.itemBase as Equipment)
                .equipmentType as EquipmentType,
            ),
        );
      }),
    );

    combineLatest([this.filteredInventory$, this.selectedItemId$])
      .pipe(
        map(
          ([inventory, id]) =>
            inventory.find((i) => i.itemInstance.id === id) ?? null,
        ),
      )
      .subscribe(this.selectedItem$);

    // combineLatest([this.selectedRecipe$, this.inventory$])
    //   .pipe(
    //     map(([recipe, inv]) =>
    //       recipe
    //         ? recipe.materials.every((mat) =>
    //             hasQuantity(inv?.inventoryItems!, mat.item.id, mat.quantity),
    //           )
    //         : false,
    //     ),
    //   )
    //   .subscribe(this.canCraftSelected$);
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
