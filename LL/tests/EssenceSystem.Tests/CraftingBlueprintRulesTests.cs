using Application.UseCases.Crafting;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace EssenceSystem.Tests;

public sealed class CraftingBlueprintRulesTests
{
    [Fact]
    public void IsCompatible_AllowsMatchingBaseRecipe()
    {
        var recipe = Recipe("recipe_ring", "Ring", "Accessory", [Form("band", "Band", "Ring")]);
        var blueprint = new BlueprintDefinition
        {
            Id = "blueprint_fury",
            BlueprintFamily = "Fury",
            AllowedBaseRecipeIds = ["recipe_ring"]
        };

        Assert.True(CraftingBlueprintRules.IsCompatible(blueprint, recipe, recipe.Forms[0]));
    }

    [Fact]
    public void IsCompatible_AllowsMatchingFormTag()
    {
        var recipe = Recipe("recipe_one_handed_weapon", "One-Handed Weapon", "Weapon", [Form("dagger", "Dagger", "Dagger")]);
        var blueprint = new BlueprintDefinition
        {
            Id = "blueprint_hive",
            BlueprintFamily = "Hive",
            AllowedRecipeTags = ["Dagger"]
        };

        Assert.True(CraftingBlueprintRules.IsCompatible(blueprint, recipe, recipe.Forms[0]));
    }

    [Fact]
    public void IsCompatible_RejectsUnmatchedRecipeAndTags()
    {
        var recipe = Recipe("recipe_relic", "Relic", "Relic", [Form("totem", "Totem", "Nature")]);
        var blueprint = new BlueprintDefinition
        {
            Id = "blueprint_aegis",
            BlueprintFamily = "Aegis",
            AllowedBaseRecipeIds = ["recipe_necklace"],
            AllowedRecipeTags = ["Shield"]
        };

        Assert.False(CraftingBlueprintRules.IsCompatible(blueprint, recipe, recipe.Forms[0]));
    }

    [Fact]
    public void ResolveOutputName_UsesSpecialOutputNameBeforeTemplate()
    {
        var recipe = Recipe("recipe_ring", "Ring", "Accessory", [Form("band", "Band", "Ring")]);
        var blueprint = new BlueprintDefinition
        {
            Id = "blueprint_fury",
            BlueprintFamily = "Fury",
            OutputNameTemplate = "{BlueprintName} {FormName}",
            SpecialOutputNames =
            [
                new BlueprintOutputNameDefinition
                {
                    BaseRecipeId = "recipe_ring",
                    FormId = "band",
                    OutputName = "Band of Fury"
                }
            ]
        };

        var name = CraftingBlueprintRules.ResolveOutputName(blueprint, recipe, recipe.Forms[0], "Band");

        Assert.Equal("Band of Fury", name);
    }

    [Fact]
    public void ResolveOutputName_UsesTemplateWhenNoSpecialOutputNameMatches()
    {
        var recipe = Recipe("recipe_two_handed_weapon", "Two-Handed Weapon", "Weapon", [Form("staff", "Staff", "Magic")]);
        var blueprint = new BlueprintDefinition
        {
            Id = "blueprint_arcane",
            BlueprintFamily = "Arcane",
            OutputNameTemplate = "{BlueprintName} {FormName}"
        };

        var name = CraftingBlueprintRules.ResolveOutputName(blueprint, recipe, recipe.Forms[0], "Staff");

        Assert.Equal("Arcane Staff", name);
    }

    private static CraftingRecipeDefinition Recipe(
        string id,
        string name,
        string tag,
        IReadOnlyList<CraftingRecipeFormDefinition> forms)
    {
        return new CraftingRecipeDefinition
        {
            Id = id,
            Name = name,
            RecipeFamily = tag,
            OutputItemType = EquipmentType.Relic,
            Tags = [tag],
            AffinityTags = [tag],
            Forms = forms
        };
    }

    private static CraftingRecipeFormDefinition Form(string id, string name, string tag)
    {
        return new CraftingRecipeFormDefinition
        {
            FormId = id,
            DisplayName = name,
            OutputItemType = EquipmentType.Relic,
            Tags = [tag]
        };
    }
}
