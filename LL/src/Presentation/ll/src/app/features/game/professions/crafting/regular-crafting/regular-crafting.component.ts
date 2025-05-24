import { AsyncPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { Recipe } from '../../../../../shared/models/profession';
import {
  BehaviorSubject,
  combineLatest,
  map,
  Observable,
  ReplaySubject,
} from 'rxjs';
import { CharacterManagerService } from '../../../../../core/services/client-side/character-manager/character-manager.service';
import { CraftingService } from '../../../../../core/services/api/crafting/crafting.service';
import { InventoryDto } from '../../../../../shared/models/Dtos/inventoryDto';
import { AttributeTypeFormatPipe } from '../../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';

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
  selector: 'app-regular-crafting',
  standalone: true,
  imports: [NgIf, NgFor, NgClass, AsyncPipe, AttributeTypeFormatPipe],
  templateUrl: './regular-crafting.component.html',
  styleUrl: './regular-crafting.component.css',
})
export class RegularCraftingComponent implements OnInit {
  @Input() recipes$!: Observable<Recipe[]>;
  @Input() inventory$!: Observable<InventoryDto>;

  private readonly selectedRecipeId$ = new BehaviorSubject<string | null>(null);
  readonly selectedRecipe$ = new ReplaySubject<Recipe | null>(1);
  readonly canCraftSelected$ = new ReplaySubject<boolean>(1);

  constructor(
    private readonly characterManager: CharacterManagerService,
    private readonly craftingService: CraftingService,
  ) {}
  ngOnInit(): void {
    combineLatest([this.recipes$, this.selectedRecipeId$])
      .pipe(map(([recipes, id]) => recipes.find((r) => r.id === id) ?? null))
      .subscribe(this.selectedRecipe$);

    combineLatest([this.selectedRecipe$, this.inventory$])
      .pipe(
        map(([recipe, inv]) =>
          recipe
            ? recipe.materials.every((mat) =>
                hasQuantity(inv?.inventoryItems!, mat.item.id, mat.quantity),
              )
            : false,
        ),
      )
      .subscribe(this.canCraftSelected$);
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

    /* optimistic client-side material removal */
    const updatedItems = consumeMaterials(items, recipe);
    this.craftingService.craftItem(recipe.id).subscribe((item) => {
      updatedItems.push(item);
      this.characterManager.setInventory({ inventoryItems: updatedItems });
    });
  }

  trackByRecipe(_: number, recipe: Recipe): string {
    return recipe.id;
  }
}
