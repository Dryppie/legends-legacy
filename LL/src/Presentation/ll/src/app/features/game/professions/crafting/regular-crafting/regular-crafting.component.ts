import { NgClass, NgFor, NgIf } from '@angular/common';
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
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../../../../shared/components/custom-components/dropdown/dropdown.component';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';
import { CraftingRecipe } from '../../../../../shared/models/crafting-v2';

@Component({
  selector: 'app-regular-crafting',
  standalone: true,
  imports: [NgIf, NgFor, NgClass, RegularButtonComponent, NumberFormatPipe, DropdownComponent],
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
  readonly filterMode = signal<'all' | 'craftable' | 'uncraftable'>('all');

  private readonly destroyRef = inject(DestroyRef);
  private readonly selectedRecipeId = signal<string | null>(null);
  readonly selectedFormId = signal<string | null>(null);
  readonly selectedBlueprintId = signal<string | null>(null);

  readonly familyRecipes = computed<CraftingRecipe[]>(() => {
    return this.recipesV2();
  });

  readonly selectedRecipe = computed<CraftingRecipe | null>(() => {
    const id = this.selectedRecipeId();
    const recipes = this.familyRecipes();
    return id
      ? (recipes.find((r) => r.id === id) ?? recipes[0] ?? null)
      : (recipes[0] ?? null);
  });

  readonly filteredRecipes = computed<CraftingRecipe[]>(() => {
    const mode = this.filterMode();
    return this.familyRecipes().filter((recipe) => {
      const canCraft = this.canCraft(recipe);
      if (mode === 'craftable') return canCraft;
      if (mode === 'uncraftable') return !canCraft;
      return true;
    });
  });

  readonly canCraftSelected = computed<boolean>(() => {
    const recipe = this.selectedRecipe();
    return recipe ? this.canCraft(recipe) && this.hasSelectedForm(recipe) : false;
  });

  readonly tierOptions = computed<number[]>(() => {
    const recipe = this.selectedRecipe();
    if (!recipe) return [];

    const min = Math.max(1, recipe.minTier);
    const max = Math.max(min, recipe.maxTier);
    return Array.from({ length: max - min + 1 }, (_, index) => min + index);
  });

  readonly availableBlueprints = computed(() => {
    const recipe = this.selectedRecipe();
    if (!recipe) return [];
    const formId = this.selectedFormId();
    return recipe.blueprints.filter(
      (blueprint) =>
        blueprint.compatibleFormIds.length === 0 ||
        (formId != null && blueprint.compatibleFormIds.includes(formId)),
    );
  });

  readonly selectedBlueprint = computed(() => {
    const id = this.selectedBlueprintId();
    return id
      ? (this.availableBlueprints().find((blueprint) => blueprint.id === id) ??
          null)
      : null;
  });

  readonly formDropdownOptions = computed<DropdownOption<string>[]>(() => {
    return (
      this.selectedRecipe()?.forms.map((form) => ({
        label: form.displayName,
        value: form.formId,
      })) ?? []
    );
  });

  readonly blueprintDropdownOptions = computed<DropdownOption<string | null>[]>(() => [
    { label: 'None', value: null },
    ...this.availableBlueprints().map((blueprint) => ({
      label: blueprint.blueprintFamily || blueprint.name,
      value: blueprint.id,
    })),
  ]);

  readonly selectedForm = computed(() => {
    const recipe = this.selectedRecipe();
    if (!recipe) return null;
    return recipe.forms.find((form) => form.formId === this.selectedFormId()) ?? null;
  });

  readonly selectedMaterialCosts = computed(() => {
    const recipe = this.selectedRecipe();
    const blueprint = this.selectedBlueprint();
    return blueprint?.materialCosts ?? recipe?.materialCosts ?? [];
  });

  readonly outputPreviewName = computed(() => {
    const recipe = this.selectedRecipe();
    if (!recipe) return '';
    const blueprint = this.selectedBlueprint();
    const form = this.selectedForm();
    if (!blueprint) return form?.displayName ?? recipe.name;

    const specialName = blueprint.specialOutputNames.find(
      (candidate) =>
        candidate.baseRecipeId.toLowerCase() === recipe.id.toLowerCase() &&
        (!form || candidate.formId.toLowerCase() === form.formId.toLowerCase()),
    );
    if (specialName) return specialName.outputName;

    const family = blueprint.blueprintFamily ?? blueprint.name.replace(/^Blueprint:\s*/i, '');
    return blueprint.outputNameTemplate
      .replace(/\{BlueprintName\}/gi, family)
      .replace(/\{FormName\}/gi, form?.displayName ?? recipe.name)
      .trim();
  });

  readonly affinityGroups = computed(() => {
    const recipe = this.selectedRecipe();
    if (!recipe) return [];

    const groups = [
      {
        label: 'Base item',
        tags: recipe.affinityTags,
      },
      {
        label: 'Form',
        tags: this.selectedForm()?.tags ?? [],
      },
      {
        label: 'Blueprint',
        tags: this.selectedBlueprint()?.tags ?? [],
      },
    ];

    const seen = new Set<string>();
    return groups
      .map((group) => ({
        ...group,
        tags: group.tags.filter((tag) => {
          const key = tag.toLowerCase();
          if (seen.has(key)) return false;
          seen.add(key);
          return true;
        }),
      }))
      .filter((group) => group.tags.length > 0);
  });

  constructor(
    private readonly inventoryState: InventoryStateService,
    private readonly craftingService: CraftingService,
  ) {
    effect(
      () => {
        this.loadRecipes(this.targetTier());
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const recipes = this.familyRecipes();
        const selected = this.selectedRecipe();
        if (!recipes.length) {
          this.selectedRecipeId.set(null);
          this.selectedFormId.set(null);
          this.selectedBlueprintId.set(null);
          return;
        }

        if (!selected || !recipes.some((recipe) => recipe.id === this.selectedRecipeId())) {
          const first = recipes[0];
          this.selectedRecipeId.set(first.id);
          this.selectedFormId.set(first.forms[0]?.formId ?? null);
          this.selectedBlueprintId.set(null);
        }
      },
      { allowSignalWrites: true },
    );

    this.craftingService.blueprintLearned$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadRecipes(this.targetTier()));
  }

  selectRecipe(recipe: CraftingRecipe): void {
    this.selectedRecipeId.set(recipe.id);
    this.selectedFormId.set(recipe.forms[0]?.formId ?? null);
    this.selectedBlueprintId.set(null);
    this.targetTier.set(Math.min(Math.max(this.targetTier(), recipe.minTier), recipe.maxTier));
  }

  setForm(formId: string): void {
    this.selectedFormId.set(formId);
    if (!this.availableBlueprints().some((blueprint) => blueprint.id === this.selectedBlueprintId())) {
      this.selectedBlueprintId.set(null);
    }
  }

  setBlueprint(blueprintId: string): void {
    this.selectedBlueprintId.set(blueprintId || null);
  }

  setFormFromDropdown(selection: DropdownSelection<string | null>): void {
    if (!selection.main) return;
    this.setForm(selection.main);
  }

  setBlueprintFromDropdown(selection: DropdownSelection<string | null>): void {
    this.selectedBlueprintId.set(selection.main);
  }

  setTargetTier(value: number): void {
    const selected = this.selectedRecipe();
    const min = selected?.minTier ?? 1;
    const max = selected?.maxTier ?? 10;
    this.targetTier.set(Math.min(Math.max(value || min, min), max));
  }

  setQuantity(value: number): void {
    this.quantity.set(Math.min(Math.max(value || 1, 1), 100));
  }

  craft(recipe: CraftingRecipe): void {
    if (!this.canCraft(recipe)) return;

    this.craftingService
      .craftItems({
        recipeId: recipe.id,
        formId: this.selectedFormId(),
        blueprintId: this.selectedBlueprintId(),
        targetTier: this.targetTier(),
        quantity: this.quantity(),
      })
      .subscribe({
        next: (result) => {
          const updatedInventory = this.consumeMaterials(
            this.inventoryState.items(),
            this.selectedMaterialCosts(),
            this.quantity(),
          );
          this.inventoryState.setInventory([...updatedInventory, ...result.createdItems]);
          this.loadRecipes(this.targetTier());
        },
        error: (err) => this.error.set(err.message ?? 'Failed to craft items.'),
      });
  }

  getOwnedQuantity(itemId: string): number {
    const inventoryItem = this.inventory().find(
      (i) => i.itemInstance.itemBase.id === itemId,
    );
    return inventoryItem?.quantity ?? 0;
  }

  requiredForBatch(required: number): number {
    return required * this.quantity();
  }

  canCraft(recipe: CraftingRecipe): boolean {
    const costs = recipe.id === this.selectedRecipe()?.id ? this.selectedMaterialCosts() : recipe.materialCosts;
    return costs.every(
      (cost) => this.getOwnedQuantity(cost.itemId) >= this.requiredForBatch(cost.required),
    );
  }

  hasSelectedForm(recipe: CraftingRecipe): boolean {
    return recipe.forms.length === 0 || recipe.forms.some((form) => form.formId === this.selectedFormId());
  }

  trackByRecipe(_: number, recipe: CraftingRecipe): string {
    return recipe.id;
  }

  trackByTier(_: number, tier: number): number {
    return tier;
  }

  recipeTierLabel(recipe: CraftingRecipe): string {
    return recipe.minTier === recipe.maxTier
      ? `T${recipe.minTier}`
      : `T${recipe.minTier}-${recipe.maxTier}`;
  }

  formatDisplayLabel(value: string | null | undefined): string {
    if (!value) return '';

    return value
      .replace(/[_-]+/g, ' ')
      .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
      .replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2')
      .trim();
  }

  private loadRecipes(targetTier: number): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.craftingService.getRecipes(targetTier).subscribe({
      next: (recipes) => {
        this.recipesV2.set(recipes);
        const familyRecipes = this.familyRecipes();
        if (!familyRecipes.some((recipe) => recipe.id === this.selectedRecipeId())) {
          this.selectedRecipeId.set(familyRecipes[0]?.id ?? null);
        }
        const selected =
          familyRecipes.find((recipe) => recipe.id === this.selectedRecipeId()) ??
          familyRecipes[0];
        if (selected && !this.hasSelectedForm(selected)) {
          this.selectedFormId.set(selected.forms[0]?.formId ?? null);
        }
        if (!this.availableBlueprints().some((blueprint) => blueprint.id === this.selectedBlueprintId())) {
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
    const remainingByItemId = new Map(
      costs.map((cost) => [cost.itemId, cost.required * quantity]),
    );

    return inventory
      .map((item) => {
        const itemId = item.itemInstance.itemBase.id;
        const remaining = remainingByItemId.get(itemId) ?? 0;
        if (remaining <= 0) return item;

        const consumed = Math.min(item.quantity, remaining);
        remainingByItemId.set(itemId, remaining - consumed);

        return {
          ...item,
          quantity: item.quantity - consumed,
        };
      })
      .filter((item) => item.quantity > 0);
  }
}
