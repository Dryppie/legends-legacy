import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import {
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  Input,
  signal,
  Signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { Recipe } from '../../../../../shared/models/profession';
import { CraftingService } from '../../../../../core/services/api/crafting/crafting.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { CharacterProfession } from '../../../../../shared/models/Dtos/characterProfession';
import { RegularButtonComponent } from '../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';
import {
  CraftingBlueprint,
  CraftingItemPreview,
  CraftingRecipe,
} from '../../../../../shared/models/crafting-v2';
import { AttributeTypeFormatPipe } from '../../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../../../shared/pipes/attributes/attribute-value-format/attribute-value-format.pipe';

@Component({
  selector: 'app-regular-crafting',
  standalone: true,
  imports: [
    NgIf,
    NgFor,
    NgClass,
    DecimalPipe,
    RegularButtonComponent,
    NumberFormatPipe,
    AttributeTypeFormatPipe,
    AttributeValueFormatPipe,
  ],
  templateUrl: './regular-crafting.component.html',
})
export class RegularCraftingComponent {
  @Input({ required: true }) recipes!: Signal<Recipe[]>;
  @Input({ required: true }) inventory!: Signal<InventoryItem[]>;
  @Input({ required: true }) characterProfession!: CharacterProfession;

  readonly recipesV2 = signal<CraftingRecipe[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly targetTier = signal(1);
  readonly quantity = signal(1);
  readonly filterMode = signal<'all' | 'craftable' | 'blueprints'>('all');
  private readonly selectedRecipeId = signal<string | null>(null);
  private readonly selectedBlueprintId = signal<string | null>(null);
  private readonly destroyRef = inject(DestroyRef);

  readonly selectedRecipe = computed(() => {
    const recipes = this.recipesV2();
    return (
      recipes.find((recipe) => recipe.id === this.selectedRecipeId()) ??
      recipes[0] ??
      null
    );
  });

  readonly selectedBlueprint = computed(() => {
    const recipe = this.selectedRecipe();
    const blueprintId = this.selectedBlueprintId();
    return (
      recipe?.blueprints.find((blueprint) => blueprint.id === blueprintId) ??
      null
    );
  });

  readonly selectedDesign = computed(
    () => this.selectedBlueprint() ?? this.selectedRecipe(),
  );

  readonly filteredRecipes = computed(() =>
    this.recipesV2().filter((recipe) => {
      if (this.filterMode() === 'blueprints')
        return recipe.blueprints.length > 0;
      if (this.filterMode() === 'craftable') return this.canCraftRecipe(recipe);
      return true;
    }),
  );

  readonly tierOptions = computed(() => {
    const recipe = this.selectedRecipe();
    if (!recipe) return [];
    return Array.from(
      { length: recipe.maxTier - recipe.minTier + 1 },
      (_, index) => recipe.minTier + index,
    );
  });

  readonly selectedMaterialCosts = computed(
    () => this.selectedDesign()?.materialCosts ?? [],
  );

  readonly canCraftSelected = computed(() => {
    const recipe = this.selectedRecipe();
    const blueprint = this.selectedBlueprint();
    if (!recipe || blueprint?.isLocked) return false;
    return this.canCraftRecipe(recipe, this.selectedMaterialCosts());
  });

  constructor(
    private readonly inventoryState: InventoryStateService,
    private readonly craftingService: CraftingService,
  ) {
    effect(() => this.loadRecipes(this.targetTier()), {
      allowSignalWrites: true,
    });
    this.craftingService.blueprintLearned$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.selectedBlueprintId.set(result.blueprintId);
        this.loadRecipes(this.targetTier());
      });
  }

  selectRecipe(recipe: CraftingRecipe): void {
    this.selectedRecipeId.set(recipe.id);
    this.selectedBlueprintId.set(null);
    this.targetTier.set(
      Math.min(Math.max(this.targetTier(), recipe.minTier), recipe.maxTier),
    );
  }

  selectBaseRecipe(): void {
    this.selectedBlueprintId.set(null);
  }

  selectBlueprint(blueprint: CraftingBlueprint): void {
    this.selectedBlueprintId.set(blueprint.id);
  }

  setTargetTier(value: number): void {
    const recipe = this.selectedRecipe();
    const min = recipe?.minTier ?? 1;
    const max = recipe?.maxTier ?? min;
    this.targetTier.set(Math.min(Math.max(value || min, min), max));
  }

  setQuantity(value: number): void {
    this.quantity.set(Math.min(Math.max(value || 1, 1), 100));
  }

  craft(): void {
    const recipe = this.selectedRecipe();
    const blueprint = this.selectedBlueprint();
    if (!recipe || !this.canCraftSelected()) return;

    this.craftingService
      .craftItems({
        recipeId: recipe.id,
        blueprintId: blueprint?.id,
        targetTier: this.targetTier(),
        quantity: this.quantity(),
      })
      .subscribe({
        next: (result) => {
          const inventory = this.consumeMaterials(
            this.inventoryState.items(),
            this.selectedMaterialCosts(),
            this.quantity(),
          );
          this.inventoryState.setInventory([
            ...inventory,
            ...result.createdItems,
          ]);
          this.loadRecipes(this.targetTier());
        },
        error: (err) => this.error.set(err.message ?? 'Failed to craft items.'),
      });
  }

  getOwnedQuantity(itemId: string): number {
    return this.inventory()
      .filter((item) => item.itemInstance.itemBase.id === itemId)
      .reduce((sum, item) => sum + item.quantity, 0);
  }

  formatDisplayLabel(value: string | null | undefined): string {
    return (value ?? '')
      .replace(/[_-]+/g, ' ')
      .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
      .trim();
  }

  trackByRecipe(_: number, recipe: CraftingRecipe): string {
    return recipe.id;
  }

  trackByBlueprint(_: number, blueprint: CraftingBlueprint): string {
    return blueprint.id;
  }

  trackByTier(_: number, tier: number): number {
    return tier;
  }

  isSelectedRecipe(recipe: CraftingRecipe): boolean {
    return this.selectedRecipeId() === recipe.id;
  }

  minimumWeaponDamage(preview: CraftingItemPreview): number {
    return Math.round(preview.magnitude * (1 - preview.magnitudeRange / 100));
  }

  maximumWeaponDamage(preview: CraftingItemPreview): number {
    return Math.round(preview.magnitude * (1 + preview.magnitudeRange / 100));
  }

  private canCraftRecipe(
    recipe: CraftingRecipe,
    costs = recipe.materialCosts,
  ): boolean {
    if (this.characterProfession.level < recipe.minimumProfessionLevel)
      return false;
    return costs.every(
      (cost) =>
        this.getOwnedQuantity(cost.itemId) >= cost.required * this.quantity(),
    );
  }

  private loadRecipes(targetTier: number): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.craftingService.getRecipes(targetTier).subscribe({
      next: (recipes) => {
        this.recipesV2.set(recipes);
        const currentBlueprintId = this.selectedBlueprintId();
        let recipe =
          recipes.find(
            (candidate) => candidate.id === this.selectedRecipeId(),
          ) ?? recipes[0];
        if (
          currentBlueprintId &&
          !recipe?.blueprints.some(
            (blueprint) => blueprint.id === currentBlueprintId,
          )
        ) {
          recipe =
            recipes.find((candidate) =>
              candidate.blueprints.some(
                (blueprint) => blueprint.id === currentBlueprintId,
              ),
            ) ?? recipe;
        }
        this.selectedRecipeId.set(recipe?.id ?? null);
        if (
          currentBlueprintId &&
          !recipe?.blueprints.some(
            (blueprint) => blueprint.id === currentBlueprintId,
          )
        ) {
          this.selectedBlueprintId.set(null);
        }
        this.isLoading.set(false);
      },
      error: (err) => {
        this.error.set(err.message ?? 'Failed to load crafting recipes.');
        this.isLoading.set(false);
      },
    });
  }

  private consumeMaterials(
    inventory: InventoryItem[],
    costs: { itemId: string; required: number }[],
    quantity: number,
  ): InventoryItem[] {
    const remaining = new Map(
      costs.map((cost) => [cost.itemId, cost.required * quantity]),
    );
    return inventory
      .map((item) => {
        const itemId = item.itemInstance.itemBase.id;
        const needed = remaining.get(itemId) ?? 0;
        const consumed = Math.min(item.quantity, needed);
        remaining.set(itemId, needed - consumed);
        return { ...item, quantity: item.quantity - consumed };
      })
      .filter((item) => item.quantity > 0);
  }
}
