import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, computed, Input, signal, Signal } from '@angular/core';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { Recipe } from '../../../../../shared/models/profession';
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
  imports: [NgIf, NgFor, NgClass, AttributeTypeFormatPipe],
  templateUrl: './regular-crafting.component.html',
})
export class RegularCraftingComponent {
  @Input({ required: true }) recipes!: Signal<Recipe[]>;
  @Input({ required: true }) inventory!: Signal<InventoryItem[]>;

  private readonly selectedRecipeId = signal<string | null>(null);
  readonly selectedRecipe = computed<Recipe | null>(() => {
    const id = this.selectedRecipeId();
    return id ? (this.recipes().find((r) => r.id === id) ?? null) : null;
  });
  readonly canCraftSelected = computed<boolean>(() => {
    const recipe = this.selectedRecipe();
    const inv = this.inventory();
    if (!recipe || !inv) return false;
    return recipe.materials.every((mat) =>
      hasQuantity(inv, mat.item.id, mat.quantity),
    );
  });

  constructor(
    private readonly characterManager: CharacterManagerService,
    private readonly craftingService: CraftingService,
  ) {}

  selectRecipe(recipe: Recipe): void {
    this.selectedRecipeId.set(recipe.id);
  }

  craft(recipe: Recipe): void {
    const inventory = this.characterManager.getInventory();
    if (!inventory) return;

    if (
      !recipe.materials.every((m) =>
        hasQuantity(inventory.inventoryItems, m.item.id, m.quantity),
      )
    ) {
      return;
    }

    const updatedItems = consumeMaterials(inventory.inventoryItems, recipe);
    this.craftingService.craftItem(recipe.id).subscribe((item) => {
      updatedItems.push(item);
      this.characterManager.setInventory({ inventoryItems: updatedItems });
    });
  }

  trackByRecipe(_: number, recipe: Recipe): string {
    return recipe.id;
  }
}
