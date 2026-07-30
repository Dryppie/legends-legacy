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
  CraftingAttributePreview,
  CraftingBlueprint,
  CraftingMaterialCost,
  CraftingRecipe,
} from '../../../../../shared/models/crafting-v2';
import { AttributeType } from '../../../../../shared/models/enums/attributeType';
import { AttributeTypeFormatPipe } from '../../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../../../shared/pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../../../../shared/components/custom-components/dropdown/dropdown.component';
import {
  TUTORIAL_ONE_HANDED_WEAPON_ITEM_BASE_IDS,
  TUTORIAL_STEP_CRAFT_EQUIPMENT,
} from '../../../../../shared/models/tutorial';
import { TutorialStateService } from '../../../../../core/services/api/tutorial/tutorial-state.service';
import { FirstPartyTourService } from '../../../../../core/services/client-side/first-party-tour/first-party-tour.service';

interface BaseAttributeDisplay {
  attributeType: AttributeType;
  baseMinimumAmount: number;
  baseMaximumAmount: number;
  resultMinimumAmount: number;
  resultMaximumAmount: number;
  blueprintMinimumChange: number;
  blueprintMaximumChange: number;
  hasBlueprintChange: boolean;
}

type RecipeFilterMode =
  | 'all'
  | 'craftable'
  | 'learned'
  | 'unlearned'
  | 'mastery';

type MobileCraftingPane = 'recipes' | 'blueprints' | 'preview';

@Component({
  selector: 'app-regular-crafting',
  imports: [
    NgIf,
    NgFor,
    NgClass,
    DecimalPipe,
    RegularButtonComponent,
    DropdownComponent,
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
  readonly filterMode = signal<RecipeFilterMode>('all');
  readonly recipeSearch = signal('');
  readonly recipeCategory = signal('all');
  readonly blueprintSearch = signal('');
  readonly blueprintFilter = signal<'all' | 'craftable' | 'locked'>('all');
  readonly mobilePane = signal<MobileCraftingPane>('recipes');
  private readonly selectedRecipeId = signal<string | null>(null);
  private readonly selectedBlueprintId = signal<string | null>(null);
  private readonly destroyRef = inject(DestroyRef);
  private readonly tutorialState = inject(TutorialStateService);
  private readonly firstPartyTour = inject(FirstPartyTourService);

  readonly isTutorialWeaponSelectionActive = computed(
    () =>
      this.tutorialState.state()?.currentStep ===
      TUTORIAL_STEP_CRAFT_EQUIPMENT,
  );

  private readonly tutorialScopedRecipes = computed(() => {
    const recipes = this.recipesV2();
    if (!this.isTutorialWeaponSelectionActive()) return recipes;

    return recipes.filter((recipe) => this.isTutorialWeaponRecipe(recipe));
  });

  readonly selectedRecipe = computed(() => {
    const recipes = this.tutorialScopedRecipes();
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

  readonly baseAttributeDisplays = computed<BaseAttributeDisplay[]>(() => {
    const recipe = this.selectedRecipe();
    const blueprint = this.selectedBlueprint();
    const basePreview = recipe?.itemPreview;
    const resultPreview = this.selectedDesign()?.itemPreview;
    if (!basePreview || !resultPreview) return [];

    const resultAttributes = new Map(
      resultPreview.attributes.map((attribute) => [
        attribute.attributeType,
        attribute,
      ]),
    );
    const blueprintTypes = new Set(
      Object.entries(blueprint?.bonusStatProfile ?? {})
        .filter(([, weight]) => weight > 0)
        .map(([attributeType]) => attributeType),
    );

    return basePreview.attributes.map((base) => {
      const result = resultAttributes.get(base.attributeType) ?? base;
      const minimumChange = result.minimumTotalAmount - base.minimumTotalAmount;
      const maximumChange = result.maximumTotalAmount - base.maximumTotalAmount;
      const minimumAddition = Math.max(0, minimumChange);
      const maximumAddition = Math.max(0, maximumChange);

      return {
        attributeType: base.attributeType,
        baseMinimumAmount: base.minimumTotalAmount,
        baseMaximumAmount: base.maximumTotalAmount,
        resultMinimumAmount: result.minimumTotalAmount,
        resultMaximumAmount: result.maximumTotalAmount,
        blueprintMinimumChange: minimumAddition,
        blueprintMaximumChange: maximumAddition,
        hasBlueprintChange:
          !!blueprint &&
          blueprintTypes.has(base.attributeType) &&
          (minimumAddition > 0.001 || maximumAddition > 0.001),
      };
    });
  });

  readonly blueprintAddedAttributes = computed<CraftingAttributePreview[]>(
    () => {
      const recipe = this.selectedRecipe();
      const blueprint = this.selectedBlueprint();
      const resultPreview = blueprint?.itemPreview;
      if (!recipe || !blueprint || !resultPreview) return [];

      const baseTypes = new Set(
        (recipe.itemPreview?.attributes ?? []).map(
          (attribute) => attribute.attributeType,
        ),
      );
      const providedTypes = new Set(
        Object.entries(blueprint.bonusStatProfile ?? {})
          .filter(([, weight]) => weight > 0)
          .map(([attributeType]) => attributeType),
      );

      return resultPreview.attributes.filter(
        (attribute) =>
          providedTypes.has(attribute.attributeType) &&
          !baseTypes.has(attribute.attributeType),
      );
    },
  );

  readonly visibleBlueprints = computed(() => {
    const recipe = this.selectedRecipe();
    if (!recipe) return [];

    const query = this.blueprintSearch().trim().toLowerCase();
    return recipe.blueprints.filter((blueprint) => {
      if (this.blueprintFilter() === 'locked' && !blueprint.isLocked)
        return false;
      if (
        this.blueprintFilter() === 'craftable' &&
        !this.canCraftBlueprint(recipe, blueprint)
      )
        return false;
      if (!query) return true;

      return [
        blueprint.name,
        blueprint.craftedItemName,
        blueprint.description,
        ...blueprint.tags,
      ].some((value) => value.toLowerCase().includes(query));
    });
  });

  readonly craftableDesignCount = computed(() => {
    const recipe = this.selectedRecipe();
    if (!recipe) return 0;

    return (
      (this.canCraftRecipe(recipe) ? 1 : 0) +
      recipe.blueprints.filter((blueprint) =>
        this.canCraftBlueprint(recipe, blueprint),
      ).length
    );
  });

  readonly lockedBlueprintCount = computed(
    () =>
      this.selectedRecipe()?.blueprints.filter(
        (blueprint) => blueprint.isLocked,
      ).length ?? 0,
  );

  readonly recipeCategories = computed(() =>
    Array.from(
      new Set(this.tutorialScopedRecipes().map((recipe) => recipe.category)),
    ).sort((left, right) => left.localeCompare(right)),
  );
  readonly recipeCategoryOptions = computed<readonly DropdownOption<string>[]>(
    () => [
      { label: 'All professions', value: 'all' },
      ...this.recipeCategories().map((category) => ({
        label: this.formatDisplayLabel(category),
        value: category,
      })),
    ],
  );

  private readonly recipeSearchMatches = computed(() => {
    const tutorialRecipes = this.tutorialScopedRecipes();
    if (this.isTutorialWeaponSelectionActive()) return tutorialRecipes;

    const queryTerms = this.recipeSearch()
      .trim()
      .toLowerCase()
      .split(/\s+/)
      .filter(Boolean);
    const category = this.recipeCategory();

    return tutorialRecipes.filter((recipe) => {
      if (category !== 'all' && recipe.category !== category) return false;
      if (!queryTerms.length) return true;

      const searchableText = [
        recipe.name,
        recipe.description,
        recipe.category,
        recipe.outputItemType,
        ...recipe.tags,
        ...recipe.affinityTags,
      ]
        .join(' ')
        .toLowerCase();

      return queryTerms.every((term) => searchableText.includes(term));
    });
  });

  readonly recipeFilterCounts = computed(() => {
    const recipes = this.recipeSearchMatches();
    return {
      all: recipes.length,
      craftable: recipes.filter((recipe) => this.canCraftAnyDesign(recipe))
        .length,
      learned: recipes.filter((recipe) => this.hasLearnedBlueprint(recipe))
        .length,
      unlearned: recipes.filter((recipe) => !this.hasLearnedBlueprint(recipe))
        .length,
      mastery: recipes.filter((recipe) => recipe.currentMasteryLevel > 0)
        .length,
    };
  });

  readonly filteredRecipes = computed(() => {
    const recipes = this.recipeSearchMatches();
    if (this.isTutorialWeaponSelectionActive()) return recipes;

    const mode = this.filterMode();
    return recipes.filter((recipe) => {
      switch (mode) {
        case 'craftable':
          return this.canCraftAnyDesign(recipe);
        case 'learned':
          return this.hasLearnedBlueprint(recipe);
        case 'unlearned':
          return !this.hasLearnedBlueprint(recipe);
        case 'mastery':
          return recipe.currentMasteryLevel > 0;
        case 'all':
        default:
          return true;
      }
    });
  });

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

  readonly blueprintSpecificMaterialCosts = computed<CraftingMaterialCost[]>(
    () => {
      const recipe = this.selectedRecipe();
      const blueprint = this.selectedBlueprint();
      if (!recipe || !blueprint) return [];

      const baseRequiredByItem = new Map(
        recipe.materialCosts.map((material) => [
          material.itemId,
          material.required,
        ]),
      );

      return blueprint.materialCosts
        .map((material) => ({
          ...material,
          required:
            material.required - (baseRequiredByItem.get(material.itemId) ?? 0),
        }))
        .filter((material) => material.required > 0);
    },
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
    this.inventoryState.load(true);
    effect(() => this.loadRecipes(this.targetTier()), {
      allowSignalWrites: true,
    });
    effect(
      () => {
        this.isTutorialWeaponSelectionActive();
        this.selectFirstVisibleRecipeIfNeeded();
      },
      { allowSignalWrites: true },
    );
    effect(
      () => {
        const tour = this.firstPartyTour.state();
        if (tour?.pageId !== 'tutorial-crafting') return;

        switch (tour.step.id) {
          case 'explain-common-base':
          case 'explain-blueprints':
            this.mobilePane.set('blueprints');
            break;
          case 'explain-item-preview':
            this.mobilePane.set('preview');
            break;
        }
      },
      { allowSignalWrites: true },
    );
    this.craftingService.blueprintLearned$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.selectedRecipeId.set(result.recipeId);
        this.selectedBlueprintId.set(result.blueprintId);
        this.loadRecipes(this.targetTier());
      });
  }

  selectRecipe(recipe: CraftingRecipe, openBlueprints = false): void {
    this.selectedRecipeId.set(recipe.id);
    this.selectedBlueprintId.set(null);
    this.blueprintSearch.set('');
    this.blueprintFilter.set('all');
    if (openBlueprints) this.mobilePane.set('blueprints');
    this.targetTier.set(
      Math.min(Math.max(this.targetTier(), recipe.minTier), recipe.maxTier),
    );
  }

  setMobilePane(pane: MobileCraftingPane): void {
    this.mobilePane.set(pane);
  }

  setRecipeFilter(mode: RecipeFilterMode): void {
    this.filterMode.set(mode);
    this.selectFirstVisibleRecipeIfNeeded();
  }

  setRecipeSearch(value: string): void {
    this.recipeSearch.set(value);
    this.selectFirstVisibleRecipeIfNeeded();
  }

  setRecipeCategory(selection: DropdownSelection<string>): void {
    this.recipeCategory.set(selection.main);
    this.selectFirstVisibleRecipeIfNeeded();
  }

  selectBaseRecipe(): void {
    this.selectedBlueprintId.set(null);
  }

  selectBlueprint(blueprint: CraftingBlueprint): void {
    this.selectedBlueprintId.set(blueprint.id);
  }

  setBlueprintSearch(value: string): void {
    this.blueprintSearch.set(value);
  }

  setTargetTier(value: number): void {
    const recipe = this.selectedRecipe();
    const min = recipe?.minTier ?? 1;
    const max = recipe?.maxTier ?? min;
    this.targetTier.set(Math.min(Math.max(value || min, min), max));
  }

  setQuantity(value: number): void {
    this.quantity.set(Math.min(Math.max(value || 1, 1), 100));
    this.selectFirstVisibleRecipeIfNeeded();
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

  isTutorialWeaponRecipe(recipe: CraftingRecipe): boolean {
    return (
      recipe.minTier === 1 &&
      TUTORIAL_ONE_HANDED_WEAPON_ITEM_BASE_IDS.has(recipe.outputItemId)
    );
  }

  isBlueprintCraftable(blueprint: CraftingBlueprint): boolean {
    const recipe = this.selectedRecipe();
    return !!recipe && this.canCraftBlueprint(recipe, blueprint);
  }

  isBaseRecipeCraftable(): boolean {
    const recipe = this.selectedRecipe();
    return !!recipe && this.canCraftRecipe(recipe);
  }

  learnedBlueprintCount(recipe: CraftingRecipe): number {
    return recipe.blueprints.filter(
      (blueprint) => blueprint.isLearned || !blueprint.isLocked,
    ).length;
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

  private canCraftBlueprint(
    recipe: CraftingRecipe,
    blueprint: CraftingBlueprint,
  ): boolean {
    return (
      !blueprint.isLocked &&
      this.canCraftRecipe(recipe, blueprint.materialCosts)
    );
  }

  canCraftAnyDesign(recipe: CraftingRecipe): boolean {
    return (
      this.canCraftRecipe(recipe) ||
      recipe.blueprints.some((blueprint) =>
        this.canCraftBlueprint(recipe, blueprint),
      )
    );
  }

  private hasLearnedBlueprint(recipe: CraftingRecipe): boolean {
    return recipe.blueprints.some(
      (blueprint) => blueprint.isLearned || !blueprint.isLocked,
    );
  }

  private selectFirstVisibleRecipeIfNeeded(): void {
    const visibleRecipes = this.filteredRecipes();
    if (
      visibleRecipes.length &&
      !visibleRecipes.some((recipe) => recipe.id === this.selectedRecipeId())
    ) {
      this.selectRecipe(visibleRecipes[0]);
    }
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
        this.selectFirstVisibleRecipeIfNeeded();
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
