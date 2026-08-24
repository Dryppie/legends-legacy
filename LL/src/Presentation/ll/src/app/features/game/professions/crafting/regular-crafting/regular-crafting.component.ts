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
import { HoverPopoverComponent } from '../../../../../shared/components/custom-components/popovers/hover-popover/hover-popover.component';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';
import {
  CraftingAttributePreview,
  CraftingBlueprint,
  CraftingItemPreview,
  CraftingMaterialCost,
  CraftingRecipe,
} from '../../../../../shared/models/crafting-v2';
import { AttributeType } from '../../../../../shared/models/enums/attributeType';
import { EquipmentType } from '../../../../../shared/models/enums/equipmentType';
import { AttributeTypeFormatPipe } from '../../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../../../shared/pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../../../../shared/components/custom-components/dropdown/dropdown.component';
import { ONBOARDING_ONE_HANDED_WEAPON_ITEM_BASE_IDS } from '../../../../../shared/models/quest';
import { QuestStateService } from '../../../../../core/services/api/quest/quest-state.service';
import { FirstPartyTourService } from '../../../../../core/services/client-side/first-party-tour/first-party-tour.service';
import { EquipmentSlotType } from '../../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../../shared/models/item';
import { Rarity } from '../../../../../shared/models/enums/rarity';
import { mapInstanceToDisplay } from '../../../../../shared/components/equipment/equipment-display';
import { AttributeModifier } from '../../../../../shared/models/Dtos/attributesDto';
import { EquipmentSetProgressComponent } from '../../../../../shared/components/equipment/equipment-set-progress/equipment-set-progress.component';

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

interface CraftedAttributeDisplay extends AttributeModifier {
  minimumAmount: number;
  maximumAmount: number;
  rollPercentage: number;
}

interface CraftedItemPreviewState {
  equipment: EquipmentInstance;
  itemPreview: CraftingItemPreview;
  masteryXpGained: number;
  craftedCount: number;
  recipeId: string;
  blueprintId: string | null;
}

type RecipeFilterMode =
  | 'all'
  | 'craftable'
  | 'learned'
  | 'unlearned'
  | 'mastery';

type MobileCraftingPane = 'recipes' | 'blueprints' | 'preview';
type RecipeEquipmentSlot = EquipmentSlotType | 'all';
type BlueprintFilterMode =
  | 'all'
  | 'ready'
  | 'craftable'
  | 'missing'
  | 'notOwned';

const EQUIPMENT_SLOT_BY_TYPE: Record<EquipmentType, EquipmentSlotType> = {
  [EquipmentType.Head]: EquipmentSlotType.Head,
  [EquipmentType.Relic]: EquipmentSlotType.Relic,
  [EquipmentType.Chest]: EquipmentSlotType.Chest,
  [EquipmentType.Necklace]: EquipmentSlotType.Necklace,
  [EquipmentType.Legs]: EquipmentSlotType.Legs,
  [EquipmentType.Ring]: EquipmentSlotType.Ring,
  [EquipmentType.OneHanded]: EquipmentSlotType.MainHand,
  [EquipmentType.TwoHanded]: EquipmentSlotType.MainHand,
  [EquipmentType.OffHand]: EquipmentSlotType.OffHand,
  [EquipmentType.Tool]: EquipmentSlotType.Tool,
};

export function getRecipeEquipmentSlot(
  outputItemType: EquipmentType,
): EquipmentSlotType {
  return EQUIPMENT_SLOT_BY_TYPE[outputItemType];
}

export function matchesRecipeSearch(
  recipe: CraftingRecipe,
  queryTerms: readonly string[],
): boolean {
  if (!queryTerms.length) return true;

  const searchableText = [
    recipe.name,
    recipe.category,
    recipe.outputItemType,
    ...recipe.tags,
    ...recipe.affinityTags,
    ...recipe.blueprints.flatMap((blueprint) => [
      blueprint.name,
      blueprint.craftedItemName,
      ...blueprint.tags,
    ]),
  ]
    .join(' ')
    .toLowerCase();

  return queryTerms.every((term) => searchableText.includes(term));
}

export function getRollPercentage(
  value: number,
  minimum: number,
  maximum: number,
): number {
  if (maximum <= minimum) return 100;
  return Math.min(
    100,
    Math.max(0, ((value - minimum) / (maximum - minimum)) * 100),
  );
}

export function matchesCraftedSelection(
  craftedRecipeId: string,
  craftedBlueprintId: string | null,
  selectedRecipeId: string | null,
  selectedBlueprintId: string | null,
): boolean {
  return (
    craftedRecipeId === selectedRecipeId &&
    craftedBlueprintId === selectedBlueprintId
  );
}

@Component({
  selector: 'app-regular-crafting',
  imports: [
    NgIf,
    NgFor,
    NgClass,
    DecimalPipe,
    RegularButtonComponent,
    HoverPopoverComponent,
    DropdownComponent,
    NumberFormatPipe,
    AttributeTypeFormatPipe,
    AttributeValueFormatPipe,
    EquipmentSetProgressComponent,
  ],
  templateUrl: './regular-crafting.component.html',
  styleUrl: './regular-crafting.component.css',
})
export class RegularCraftingComponent {
  @Input({ required: true }) recipes!: Signal<Recipe[]>;
  @Input({ required: true }) inventory!: Signal<InventoryItem[]>;
  @Input({ required: true }) characterProfession!: CharacterProfession;

  /** Panel styling for the portaled material-source tooltip. */
  readonly materialTooltipClass =
    'll-panel w-64 max-w-[calc(100vw-2rem)] bg-texture p-3 text-left text-xs font-normal normal-case tracking-normal text-zinc-200 shadow-xl';

  readonly recipesV2 = signal<CraftingRecipe[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly targetTier = signal(1);
  readonly quantity = signal(1);
  readonly filterMode = signal<RecipeFilterMode>('all');
  readonly recipeSearch = signal('');
  readonly recipeCategory = signal('all');
  readonly recipeSubcategory = signal('all');
  readonly recipeEquipmentSlot = signal<RecipeEquipmentSlot>('all');
  readonly blueprintSearch = signal('');
  readonly blueprintFilter = signal<BlueprintFilterMode>('all');
  readonly learningBlueprintId = signal<string | null>(null);
  readonly mobilePane = signal<MobileCraftingPane>('recipes');
  readonly craftedItem = signal<CraftedItemPreviewState | null>(null);
  private readonly selectedRecipeId = signal<string | null>(null);
  private readonly selectedBlueprintId = signal<string | null>(null);
  private readonly destroyRef = inject(DestroyRef);
  private readonly questState = inject(QuestStateService);
  private readonly firstPartyTour = inject(FirstPartyTourService);

  readonly isOnboardingWeaponSelectionActive = computed(
    () =>
      this.questState.pinnedOnboardingObjective()?.type === 'EquipmentCrafted',
  );

  private readonly onboardingScopedRecipes = computed(() => {
    const recipes = this.recipesV2();
    if (!this.isOnboardingWeaponSelectionActive()) return recipes;

    return recipes.filter((recipe) => this.isOnboardingWeaponRecipe(recipe));
  });

  readonly selectedRecipe = computed(() => {
    const recipes = this.onboardingScopedRecipes();
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
  readonly craftedAttributes = computed<CraftedAttributeDisplay[]>(() => {
    const crafted = this.craftedItem();
    if (!crafted) return [];

    const ranges = new Map(
      crafted.itemPreview.attributes.map((attribute) => [
        attribute.attributeType,
        attribute,
      ]),
    );

    return mapInstanceToDisplay(crafted.equipment).attributes.map(
      (attribute) => {
        const range = ranges.get(attribute.attributeType);
        const minimumAmount = range?.minimumTotalAmount ?? attribute.amount;
        const maximumAmount = range?.maximumTotalAmount ?? attribute.amount;
        return {
          ...attribute,
          minimumAmount,
          maximumAmount,
          rollPercentage: getRollPercentage(
            attribute.amount,
            minimumAmount,
            maximumAmount,
          ),
        };
      },
    );
  });
  readonly craftedPotentialPercentage = computed(() => {
    const crafted = this.craftedItem();
    if (!crafted) return 0;
    return getRollPercentage(
      crafted.equipment.potential ?? 0,
      crafted.itemPreview.minimumStartingPotential,
      crafted.itemPreview.maximumStartingPotential,
    );
  });

  usesV16ItemPresentation(statModelVersion?: number | null): boolean {
    return (statModelVersion ?? 15) >= 16;
  }
  readonly possibleTemperingAttributes = computed(() => {
    const design = this.selectedDesign();
    if (!design) return [];

    return Array.from(
      new Set([
        ...design.primaryTemperingStats,
        ...design.secondaryTemperingStats,
      ]),
    );
  });

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
    return recipe.blueprints
      .filter((blueprint) => {
        const owned = this.blueprintOwnedQuantity(blueprint) > 0;
        const craftable =
          !blueprint.isLocked && this.canCraftBlueprint(recipe, blueprint, 1);

        switch (this.blueprintFilter()) {
          case 'ready':
            if (!blueprint.isLocked || !owned) return false;
            break;
          case 'craftable':
            if (!craftable) return false;
            break;
          case 'missing':
            if (blueprint.isLocked || craftable) return false;
            break;
          case 'notOwned':
            if (!blueprint.isLocked || owned) return false;
            break;
        }

        if (!query) return true;

        return [
          blueprint.name,
          blueprint.craftedItemName,
          ...blueprint.tags,
        ].some((value) => value.toLowerCase().includes(query));
      })
      .sort((left, right) => left.name.localeCompare(right.name));
  });

  readonly readyToLearnBlueprints = computed(() =>
    this.visibleBlueprints().filter(
      (blueprint) =>
        blueprint.isLocked && this.blueprintOwnedQuantity(blueprint) > 0,
    ),
  );

  readonly learnedBlueprints = computed(() =>
    this.visibleBlueprints().filter((blueprint) => !blueprint.isLocked),
  );

  readonly notOwnedBlueprints = computed(() =>
    this.visibleBlueprints().filter(
      (blueprint) =>
        blueprint.isLocked && this.blueprintOwnedQuantity(blueprint) === 0,
    ),
  );

  readonly readyToLearnBlueprintCount = computed(
    () =>
      this.selectedRecipe()?.blueprints.filter(
        (blueprint) =>
          blueprint.isLocked && this.blueprintOwnedQuantity(blueprint) > 0,
      ).length ?? 0,
  );

  readonly craftableBlueprintCount = computed(() => {
    const recipe = this.selectedRecipe();
    if (!recipe) return 0;

    return recipe.blueprints.filter(
      (blueprint) =>
        !blueprint.isLocked && this.canCraftBlueprint(recipe, blueprint, 1),
    ).length;
  });

  readonly missingMaterialsBlueprintCount = computed(() => {
    const recipe = this.selectedRecipe();
    if (!recipe) return 0;

    return recipe.blueprints.filter(
      (blueprint) =>
        !blueprint.isLocked && !this.canCraftBlueprint(recipe, blueprint, 1),
    ).length;
  });

  readonly notOwnedBlueprintCount = computed(
    () =>
      this.selectedRecipe()?.blueprints.filter(
        (blueprint) =>
          blueprint.isLocked && this.blueprintOwnedQuantity(blueprint) === 0,
      ).length ?? 0,
  );

  readonly recipeCategories = computed(() =>
    Array.from(
      new Set(this.onboardingScopedRecipes().map((recipe) => recipe.category)),
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
  readonly recipeSubcategoryOptions = computed<
    readonly DropdownOption<string>[]
  >(() => {
    switch (this.recipeCategory()) {
      case 'ArmorForging':
        return [
          { label: 'All armor weights', value: 'all' },
          { label: 'Heavy', value: 'HeavyArmor' },
          { label: 'Medium', value: 'MediumArmor' },
          { label: 'Light', value: 'LightArmor' },
          { label: 'Cloth', value: 'ClothArmor' },
        ];
      case 'WeaponSmithing':
        return [
          { label: 'All weapon types', value: 'all' },
          { label: 'One Handed', value: 'OneHanded' },
          { label: 'Two Handed', value: 'TwoHanded' },
          { label: 'Off Hand', value: 'OffHand' },
        ];
      default:
        return [];
    }
  });
  readonly recipeSubcategoryLabel = computed(
    () => this.recipeSubcategoryOptions()[0]?.label ?? 'All types',
  );
  readonly recipeEquipmentSlotOptions = computed<
    readonly DropdownOption<RecipeEquipmentSlot>[]
  >(() => {
    const availableSlots = new Set(
      this.onboardingScopedRecipes().map((recipe) =>
        getRecipeEquipmentSlot(recipe.outputItemType),
      ),
    );
    const slotOrder: readonly EquipmentSlotType[] = [
      EquipmentSlotType.Head,
      EquipmentSlotType.Chest,
      EquipmentSlotType.Legs,
      EquipmentSlotType.MainHand,
      EquipmentSlotType.OffHand,
      EquipmentSlotType.Relic,
      EquipmentSlotType.Necklace,
      EquipmentSlotType.Ring,
      EquipmentSlotType.Tool,
    ];

    return [
      { label: 'All equipment slots', value: 'all' },
      ...slotOrder
        .filter((slot) => availableSlots.has(slot))
        .map((slot) => ({
          label: this.formatDisplayLabel(slot),
          value: slot,
        })),
    ];
  });

  private readonly recipeSearchMatches = computed(() => {
    const onboardingRecipes = this.onboardingScopedRecipes();
    if (this.isOnboardingWeaponSelectionActive()) return onboardingRecipes;

    const queryTerms = this.recipeSearch()
      .trim()
      .toLowerCase()
      .split(/\s+/)
      .filter(Boolean);
    const category = this.recipeCategory();
    const subcategory = this.recipeSubcategory();
    const equipmentSlot = this.recipeEquipmentSlot();

    return onboardingRecipes.filter((recipe) => {
      if (category !== 'all' && recipe.category !== category) return false;
      if (
        equipmentSlot !== 'all' &&
        getRecipeEquipmentSlot(recipe.outputItemType) !== equipmentSlot
      )
        return false;
      if (
        subcategory !== 'all' &&
        !this.matchesRecipeSubcategory(recipe, category, subcategory)
      )
        return false;
      return matchesRecipeSearch(recipe, queryTerms);
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
    if (this.isOnboardingWeaponSelectionActive()) return recipes;

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
    readonly inventoryState: InventoryStateService,
    private readonly craftingService: CraftingService,
  ) {
    this.inventoryState.load(true);
    effect(() => this.loadRecipes(this.targetTier()), {
      allowSignalWrites: true,
    });
    effect(
      () => {
        const crafted = this.craftedItem();
        if (!crafted) return;
        if (
          !matchesCraftedSelection(
            crafted.recipeId,
            crafted.blueprintId,
            this.selectedRecipeId(),
            this.selectedBlueprintId(),
          )
        ) {
          this.craftedItem.set(null);
        }
      },
      { allowSignalWrites: true },
    );
    effect(
      () => {
        this.isOnboardingWeaponSelectionActive();
        this.selectFirstVisibleRecipeIfNeeded();
      },
      { allowSignalWrites: true },
    );
    effect(
      () => {
        const tour = this.firstPartyTour.state();
        if (tour?.pageId !== 'tutorial-crafting') return;

        switch (tour.step.id) {
          case 'choose-tutorial-weapon':
            this.mobilePane.set('recipes');
            break;
          case 'explain-common-base':
          case 'explain-blueprints':
            this.mobilePane.set('blueprints');
            break;
          case 'explain-item-preview':
          case 'craft-tutorial-weapon':
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
    this.recipeSubcategory.set('all');
    this.selectFirstVisibleRecipeIfNeeded();
  }

  setRecipeSubcategory(selection: DropdownSelection<string>): void {
    this.recipeSubcategory.set(selection.main);
    this.selectFirstVisibleRecipeIfNeeded();
  }

  setRecipeEquipmentSlot(
    selection: DropdownSelection<RecipeEquipmentSlot>,
  ): void {
    this.recipeEquipmentSlot.set(selection.main);
    this.selectFirstVisibleRecipeIfNeeded();
  }

  selectBaseRecipe(openPreview = false): void {
    this.selectedBlueprintId.set(null);
    if (
      openPreview &&
      this.isActiveCraftingTutorialStep('explain-common-base')
    ) {
      this.mobilePane.set('preview');
    }
  }

  selectBlueprint(blueprint: CraftingBlueprint): void {
    this.selectedBlueprintId.set(blueprint.id);
  }

  learnBlueprint(blueprint: CraftingBlueprint): void {
    const recipe = this.selectedRecipe();
    const inventoryItem = this.blueprintInventoryItem(blueprint);
    if (
      !recipe ||
      !blueprint.isLocked ||
      !inventoryItem ||
      this.learningBlueprintId()
    ) {
      return;
    }

    this.learningBlueprintId.set(blueprint.id);
    this.error.set(null);
    this.craftingService
      .learnBlueprint(inventoryItem.itemInstance.id, recipe.id)
      .subscribe({
        next: (result) => {
          this.inventoryState.applyVersionedInventoryDelta(result, () =>
            this.inventoryState.decrementItem(inventoryItem.itemInstance.id, 1),
          );
          this.learningBlueprintId.set(null);
        },
        error: (err) => {
          this.error.set(err.message ?? 'Failed to learn blueprint.');
          this.learningBlueprintId.set(null);
        },
      });
  }

  blueprintOwnedQuantity(blueprint: CraftingBlueprint): number {
    return this.inventoryState
      .items()
      .filter((item) => item.itemInstance.itemBase.id === blueprint.itemId)
      .reduce((total, item) => total + item.quantity, 0);
  }

  blueprintSourceLabel(blueprint: CraftingBlueprint): string {
    return [blueprint.sourceType, blueprint.sourceId]
      .filter((value): value is string => !!value)
      .map((value) => this.formatDisplayLabel(value))
      .join(' · ');
  }

  blueprintAvailabilityLabel(blueprint: CraftingBlueprint): string {
    const quantity = this.blueprintOwnedQuantity(blueprint);
    return (
      quantity +
      ' ' +
      (quantity === 1 ? 'blueprint' : 'blueprints') +
      ' · uses 1'
    );
  }

  missingBlueprintMaterialsLabel(blueprint: CraftingBlueprint): string {
    const missingMaterials = blueprint.materialCosts
      .filter(
        (material) =>
          this.getOwnedQuantity(material.itemId) < material.required,
      )
      .slice(0, 2)
      .map(
        (material) =>
          material.name +
          ' ' +
          this.getOwnedQuantity(material.itemId) +
          '/' +
          material.required,
      );

    if (missingMaterials.length) return missingMaterials.join(' · ');
    return (
      'Crafting level ' +
      (this.selectedRecipe()?.minimumProfessionLevel ?? 1) +
      ' required'
    );
  }

  setBlueprintSearch(value: string): void {
    this.blueprintSearch.set(value);
  }

  private blueprintInventoryItem(
    blueprint: CraftingBlueprint,
  ): InventoryItem | null {
    return (
      this.inventoryState
        .items()
        .find(
          (item) =>
            item.quantity > 0 &&
            item.itemInstance.itemBase.id === blueprint.itemId,
        ) ?? null
    );
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
    const blueprintId = blueprint?.id ?? null;
    const itemPreview = this.selectedDesign()?.itemPreview;
    if (!recipe || !itemPreview || !this.canCraftSelected()) return;

    this.isLoading.set(true);
    this.error.set(null);

    this.craftingService
      .craftItems({
        recipeId: recipe.id,
        blueprintId: blueprint?.id,
        targetTier: this.targetTier(),
        quantity: this.quantity(),
      })
      .subscribe({
        next: (versionedResult) => {
          const result = versionedResult.data;
          const applied = this.inventoryState.applyVersionedInventoryDelta(
            versionedResult,
            () => {
              const inventory = this.consumeMaterials(
                this.inventoryState.items(),
                this.selectedMaterialCosts(),
                this.quantity(),
              );
              this.inventoryState.setInventory([
                ...inventory,
                ...result.createdItems,
              ]);
            },
          );
          if (!applied) {
            this.isLoading.set(false);
            return;
          }
          const newestItem =
            result.createdItems[result.createdItems.length - 1];
          const equipment = newestItem?.itemInstance as
            | EquipmentInstance
            | undefined;
          if (
            newestItem &&
            equipment?.equipmentBase &&
            matchesCraftedSelection(
              recipe.id,
              blueprintId,
              this.selectedRecipeId(),
              this.selectedBlueprintId(),
            )
          ) {
            this.craftedItem.set({
              equipment,
              itemPreview,
              masteryXpGained: result.masteryXpGained,
              craftedCount: result.createdItems.length,
              recipeId: recipe.id,
              blueprintId,
            });
            this.mobilePane.set('preview');
          }
          this.loadRecipes(this.targetTier());
        },
        error: (err) => {
          this.error.set(err.message ?? 'Failed to craft items.');
          this.isLoading.set(false);
        },
      });
  }

  dismissCraftedItem(): void {
    this.craftedItem.set(null);
  }

  craftedItemMeta(crafted: CraftedItemPreviewState): string {
    return [
      this.formatDisplayLabel(crafted.equipment.equipmentBase.equipmentType),
      `${this.formatDisplayLabel(crafted.equipment.quality)} quality`,
      `crafted at level ${this.characterProfession.level}`,
    ].join(' · ');
  }

  rarityClass(rarity: Rarity): string {
    switch (rarity) {
      case Rarity.Common:
        return 'll-rarity-common';
      case Rarity.Uncommon:
        return 'll-rarity-uncommon';
      case Rarity.Rare:
        return 'll-rarity-rare';
      case Rarity.Epic:
        return 'll-rarity-epic';
      case Rarity.Unique:
        return 'll-rarity-unique';
      case Rarity.Legendary:
        return 'll-rarity-legendary';
      case Rarity.Legacy:
        return 'll-rarity-legacy';
      default:
        return 'text-primary';
    }
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

  isOnboardingWeaponRecipe(recipe: CraftingRecipe): boolean {
    return (
      recipe.minTier === 1 &&
      ONBOARDING_ONE_HANDED_WEAPON_ITEM_BASE_IDS.has(recipe.outputItemId)
    );
  }

  isBlueprintCraftable(blueprint: CraftingBlueprint): boolean {
    const recipe = this.selectedRecipe();
    return !!recipe && this.canCraftBlueprint(recipe, blueprint, 1);
  }

  isBaseRecipeCraftable(): boolean {
    const recipe = this.selectedRecipe();
    return !!recipe && this.canCraftRecipe(recipe, recipe.materialCosts, 1);
  }

  learnedBlueprintCount(recipe: CraftingRecipe): number {
    return recipe.blueprints.filter(
      (blueprint) => blueprint.isLearned || !blueprint.isLocked,
    ).length;
  }

  private canCraftRecipe(
    recipe: CraftingRecipe,
    costs = recipe.materialCosts,
    quantity = this.quantity(),
  ): boolean {
    if (this.characterProfession.level < recipe.minimumProfessionLevel)
      return false;
    return costs.every(
      (cost) => this.getOwnedQuantity(cost.itemId) >= cost.required * quantity,
    );
  }

  private canCraftBlueprint(
    recipe: CraftingRecipe,
    blueprint: CraftingBlueprint,
    quantity = this.quantity(),
  ): boolean {
    return (
      !blueprint.isLocked &&
      this.canCraftRecipe(recipe, blueprint.materialCosts, quantity)
    );
  }

  canCraftAnyDesign(recipe: CraftingRecipe): boolean {
    return (
      this.canCraftRecipe(recipe, recipe.materialCosts, 1) ||
      recipe.blueprints.some((blueprint) =>
        this.canCraftBlueprint(recipe, blueprint, 1),
      )
    );
  }

  private hasLearnedBlueprint(recipe: CraftingRecipe): boolean {
    return recipe.blueprints.some(
      (blueprint) => blueprint.isLearned || !blueprint.isLocked,
    );
  }

  private isActiveCraftingTutorialStep(stepId: string): boolean {
    const tour = this.firstPartyTour.state();
    return tour?.pageId === 'tutorial-crafting' && tour.step.id === stepId;
  }

  private matchesRecipeSubcategory(
    recipe: CraftingRecipe,
    category: string,
    subcategory: string,
  ): boolean {
    if (category === 'ArmorForging') {
      return recipe.tags.includes(subcategory);
    }

    if (category === 'WeaponSmithing') {
      return recipe.outputItemType === subcategory;
    }

    return true;
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
