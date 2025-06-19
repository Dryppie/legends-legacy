using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Seeds.JsonSeeding.Dtos.Recipes;
using Persistence.LL.Seeds.JsonSeeding.JsonConverters;

namespace Persistence.LL.Seeds.JsonSeeding;
public static class DbJsonSeeder
{

    public static async Task RunAsync(IDbContext ctx)
    {
        //if (await ctx.Recipes.AnyAsync()) return;

        var opt = new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter(), new ItemBaseConverter() } };
        await SeedBaseItems(ctx, opt);
        await SeedRecipes(ctx, opt);
        await ctx.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task SeedBaseItems(IDbContext ctx, JsonSerializerOptions opt)
    {
        var itemPath = Path.Combine(AppContext.BaseDirectory, "Data", "items.json");
        var itemJson = await File.ReadAllTextAsync(itemPath);
        var items = JsonSerializer.Deserialize<List<ItemBase>>(itemJson, opt)!;

        foreach (var item in items)
        {
            var existing = await ctx.ItemBases.FirstOrDefaultAsync(x => x.Id == item.Id);

            if (item is EquipmentBase equipment)
            {
                foreach (var attr in equipment.AttributeModifiers)
                {
                    attr.ItemBaseId = item.Id;
                }
            }

            if (existing == null)
            {
                ctx.ItemBases.Add(item); // New item
            }
            else
            {
                ctx.GetEntry(existing).CurrentValues.SetValues(item); // Update existing, but not attributes
            }
        }
    }

    private static async Task SeedRecipes(IDbContext ctx, JsonSerializerOptions opt)
    {
        var recipePath = Path.Combine(AppContext.BaseDirectory, "Data", "recipes.json");
        var recipeJson = await File.ReadAllTextAsync(recipePath);
        var dtos = JsonSerializer.Deserialize<List<RecipeDto>>(recipeJson, opt)!;

        foreach (var dto in dtos)
        {
            var recipe = dto.ToEntity();
            var existing = await ctx.Recipes.FirstOrDefaultAsync(r => r.Id == recipe.Id);

            if (existing == null)
            {
                ctx.Recipes.Add(recipe);
            }
            else
            {
                ctx.GetEntry(existing).CurrentValues.SetValues(recipe); // Update existing, but not materials
            }
        }
    }
}