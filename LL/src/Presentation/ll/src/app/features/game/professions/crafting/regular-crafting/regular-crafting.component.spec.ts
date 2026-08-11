import { EquipmentSlotType } from '../../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { CraftingRecipe } from '../../../../../shared/models/crafting-v2';
import { EquipmentType } from '../../../../../shared/models/enums/equipmentType';
import {
  getRecipeEquipmentSlot,
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
