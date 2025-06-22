import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, computed, Input, signal, Signal } from '@angular/core';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { Recipe } from '../../../../../shared/models/profession';
import { CraftingService } from '../../../../../core/services/api/crafting/crafting.service';
import { AttributeTypeFormatPipe } from '../../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { CharacterProfession } from '../../../../../shared/models/Dtos/characterProfession';
import { RegularButtonComponent } from '../../../../../shared/components/buttons/regular-button/regular-button.component';

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
  imports: [
    NgIf,
    NgFor,
    NgClass,
    AttributeTypeFormatPipe,
    RegularButtonComponent,
  ],
  templateUrl: './regular-crafting.component.html',
})
export class RegularCraftingComponent {
  @Input({ required: true }) recipes!: Signal<Recipe[]>;
  @Input({ required: true }) inventory!: Signal<InventoryItem[]>;
  @Input({ required: true }) characterProfession!: CharacterProfession;

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
    private readonly inventoryState: InventoryStateService,
    private readonly craftingService: CraftingService,
  ) {}

  meetsLevelRequirement(recipe: Recipe): boolean {
    return this.characterProfession.level >= recipe.levelRequirement;
  }

  selectRecipe(recipe: Recipe): void {
    this.selectedRecipeId.set(recipe.id);
  }

  craft(recipe: Recipe): void {
    const items = this.inventoryState.items();
    if (!items) return;

    if (
      !recipe.materials.every((m) => hasQuantity(items, m.item.id, m.quantity))
    ) {
      return;
    }

    const updatedItems = consumeMaterials(items, recipe);
    const backup = [...this.inventoryState.items()];

    this.craftingService.craftItem(recipe.id).subscribe({
      next: (item) => {
        this.inventoryState.setInventory([...updatedItems, item]);
      },
      error: () => {
        this.inventoryState.setInventory(backup);
      },
    });
  }

  getOwnedQuantity(itemId: string): number {
    const inventoryItem = this.inventory().find(
      (i) => i.itemInstance.itemBase.id === itemId,
    );
    return inventoryItem?.quantity ?? 0;
  }

  trackByRecipe(_: number, recipe: Recipe): string {
    return recipe.id;
  }
}
