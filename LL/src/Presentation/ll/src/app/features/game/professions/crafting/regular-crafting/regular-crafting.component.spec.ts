import { EquipmentSlotType } from '../../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { CraftingRecipe } from '../../../../../shared/models/crafting-v2';
import { EquipmentType } from '../../../../../shared/models/enums/equipmentType';
import {
  getRollPercentage,
  getRecipeEquipmentSlot,
  matchesCraftedSelection,
  matchesRecipeSearch,
} from './regular-crafting.component';

describe('regular crafting recipe filtering', () => {
  it('finds every recipe containing a matching Blueprint variant', () => {
    const recipes = [
      recipeWithBlueprint('Heavy Helm', 'Blueprint: Fury', 'Fury Heavy Helm'),
      recipeWithBlueprint('Great Sword', 'Blueprint: Fury', 'Fury Great Sword'),
      recipeWithBlueprint(
        'Medium Helm',
        'Blueprint: Arcane',
        'Arcane Medium Helm',
      ),
    ];

    const matches = recipes.filter((recipe) =>
      matchesRecipeSearch(recipe, ['fury']),
    );

    expect(matches.map((recipe) => recipe.name)).toEqual([
      'Heavy Helm',
      'Great Sword',
    ]);
  });

  it('matches search terms across the recipe and Blueprint fields', () => {
    const recipe = recipeWithBlueprint(
      'Heavy Helm',
      'Blueprint: Fury',
      'Fury Heavy Helm',
    );

    expect(matchesRecipeSearch(recipe, ['fury', 'helm'])).toBeTrue();
    expect(matchesRecipeSearch(recipe, ['arcane'])).toBeFalse();
  });

  it('groups one- and two-handed recipes into the Main Hand slot', () => {
    expect(getRecipeEquipmentSlot(EquipmentType.OneHanded)).toBe(
      EquipmentSlotType.MainHand,
    );
    expect(getRecipeEquipmentSlot(EquipmentType.TwoHanded)).toBe(
      EquipmentSlotType.MainHand,
    );
    expect(getRecipeEquipmentSlot(EquipmentType.OffHand)).toBe(
      EquipmentSlotType.OffHand,
    );
    expect(getRecipeEquipmentSlot(EquipmentType.Head)).toBe(
      EquipmentSlotType.Head,
    );
  });

  it('positions crafted rolls within their preview range', () => {
    expect(getRollPercentage(93, 93, 124)).toBe(0);
    expect(getRollPercentage(124, 93, 124)).toBe(100);
    expect(getRollPercentage(108.5, 93, 124)).toBe(50);
    expect(getRollPercentage(80, 93, 124)).toBe(0);
    expect(getRollPercentage(130, 93, 124)).toBe(100);
  });

  it('treats fixed rolls as complete', () => {
    expect(getRollPercentage(12, 12, 12)).toBe(100);
  });

  it('keeps a crafted result only for its exact recipe and Blueprint selection', () => {
    expect(
      matchesCraftedSelection('recipe-a', null, 'recipe-a', null),
    ).toBeTrue();
    expect(
      matchesCraftedSelection(
        'recipe-a',
        'blueprint-a',
        'recipe-a',
        'blueprint-a',
      ),
    ).toBeTrue();
    expect(
      matchesCraftedSelection('recipe-a', null, 'recipe-b', null),
    ).toBeFalse();
    expect(
      matchesCraftedSelection(
        'recipe-a',
        'blueprint-a',
        'recipe-a',
        'blueprint-b',
      ),
    ).toBeFalse();
  });
});

function recipeWithBlueprint(
  recipeName: string,
  blueprintName: string,
  craftedItemName: string,
): CraftingRecipe {
  return {
    name: recipeName,
    description: `Craft a ${recipeName}.`,
    category: 'ArmorForging',
    outputItemType: EquipmentType.Head,
    tags: [],
    affinityTags: [],
    blueprints: [
      {
        name: blueprintName,
        craftedItemName,
        description: '',
        tags: [],
      },
    ],
  } as unknown as CraftingRecipe;
}
