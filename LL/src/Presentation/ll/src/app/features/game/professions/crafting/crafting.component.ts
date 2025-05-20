import { Component, OnInit } from '@angular/core';
import { ProfessionHeaderComponent } from '../../../../shared/components/professions/profession-header/profession-header.component';
import { AsyncPipe, NgClass, NgFor, NgIf } from '@angular/common';
import {
  CraftingProfession,
  CraftingQueueItem,
  CraftingQueueStatus,
  Recipe,
} from '../../../../shared/models/profession';
import {
  BehaviorSubject,
  combineLatest,
  map,
  of,
  shareReplay,
  Subject,
  switchMap,
  take,
} from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { ProfessionsService } from '../../../../core/services/api/professions/professions.service';
import { CharacterActionsService } from '../../../../core/services/api/character-actions/character-actions.service';
import { CharacterManagerService } from '../../../../core/services/client-side/character-manager/character-manager.service';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { InventoryService } from '../../../../core/services/api/inventory/inventory.service';

function hasQuantity(
  inv: InventoryItem[],
  itemBaseId: string,
  required: number,
): boolean {
  const found = inv.find((ii) => ii.itemInstance.itemBase.id === itemBaseId);
  return found ? found.quantity >= required : false;
}

function consumeMaterials(
  items: InventoryItem[],
  recipe: Recipe,
): InventoryItem[] {
  return items.map((ii) => {
    const mat = recipe.materials.find(
      (m) => m.item.id === ii.itemInstance.itemBase.id,
    );
    return mat ? { ...ii, quantity: ii.quantity - mat.quantity } : ii;
  });
}

@Component({
  selector: 'app-crafting',
  standalone: true,
  imports: [ProfessionHeaderComponent, NgFor, NgIf, AsyncPipe],
  templateUrl: './crafting.component.html',
  styleUrl: './crafting.component.css',
})
export class CraftingComponent implements OnInit {
  private readonly destroy$ = new Subject<void>();

  readonly profession$;
  readonly recipes$;

  private readonly selectedRecipeId$ = new BehaviorSubject<string | null>(null);
  readonly selectedRecipe$;

  readonly currentAction$;

  readonly inventory$;

  // Stub until you wire real actions/queue in the service
  readonly craftingQueue$ = new BehaviorSubject<CraftingQueueItem[]>([]);
  readonly canCraftSelected$;

  // ────────────────────────────────────── ctor/di ─────────────────────────────
  constructor(
    private readonly route: ActivatedRoute,
    private readonly professionService: ProfessionsService,
    private readonly characterActionService: CharacterActionsService,
    private readonly characterManager: CharacterManagerService,
    private readonly inventoryService: InventoryService,
  ) {
    this.profession$ = this.route.paramMap.pipe(
      map((p) => p.get('id') ?? ''),
      switchMap(async (id) => this.professionService.getProfessionById(id)),
      shareReplay(1),
    );
    this.recipes$ = this.profession$.pipe(
      map((p) => (p as CraftingProfession).recipes),
    );
    this.selectedRecipe$ = combineLatest([
      this.recipes$,
      this.selectedRecipeId$,
    ]).pipe(map(([recipes, id]) => recipes.find((r) => r.id === id) ?? null));

    this.currentAction$ = this.characterActionService.currentAction$;

    this.inventory$ = this.characterManager.inventory$.pipe(
      switchMap(
        (invDto) =>
          invDto // already cached?
            ? of(invDto) // → just pass it through
            : this.inventoryService.getInventory(), // → make a network call
      ),
      shareReplay(1), // every subscriber sees the same value
    );
    this.canCraftSelected$ = combineLatest([
      this.selectedRecipe$,
      this.inventory$,
    ]).pipe(
      map(([recipe, inv]) =>
        recipe
          ? recipe.materials.every((mat) =>
              hasQuantity(inv?.inventoryItems!, mat.item.id, mat.quantity),
            )
          : false,
      ),
    );
  }

  // ───────────────────────── component lifecycle / housekeeping ───────────────
  ngOnInit(): void {
    // No imperative subscriptions needed – everything is async-piped in the view.
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  selectRecipe(recipe: Recipe): void {
    this.selectedRecipeId$.next(recipe.id);
  }

  craft(recipe: Recipe): void {
    // take the latest inventory once, synchronously
    const inventory = this.characterManager.getInventory();
    if (!inventory) return;
    const items = inventory.inventoryItems;
    if (
      !recipe.materials.every((m) => hasQuantity(items, m.item.id, m.quantity))
    ) {
      return; // safety net – shouldn’t happen if button was disabled
    }

    const queueItem: CraftingQueueItem = {
      id: crypto.randomUUID(),
      recipe,
      startedAt: new Date(),
      status: CraftingQueueStatus.Queued,
    };

    /* optimistic queue */
    this.craftingQueue$.next([...this.craftingQueue$.value, queueItem]);

    /* optimistic client-side material removal */
    const updatedItems = consumeMaterials(items, recipe);
    this.characterManager.setInventory({ inventoryItems: updatedItems });

    /* TODO: call backend to actually start the craft */
  }

  cancelCraft(queueItem: CraftingQueueItem): void {
    // TODO: cancel via service
    this.craftingQueue$.next(
      this.craftingQueue$.value.filter((r) => r.id !== queueItem.id),
    );
  }

  trackByRecipe(_: number, recipe: Recipe): string {
    return recipe.id;
  }
}
