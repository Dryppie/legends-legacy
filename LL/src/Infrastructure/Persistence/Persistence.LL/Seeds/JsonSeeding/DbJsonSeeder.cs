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
        if (await ctx.Recipes.AnyAsync()) return;

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
        foreach (var item in items.OfType<EquipmentBase>())
        {
            foreach (var attribute in item.AttributeModifiers)
            {
                attribute.ItemBaseId = item.Id;
            }
        }

        ctx.ItemBases.AddRange(items);
    }

    private static async Task SeedRecipes(IDbContext ctx, JsonSerializerOptions opt)
    {
        var recipePath = Path.Combine(AppContext.BaseDirectory, "Data", "recipes.json");
        var recipeJson = await File.ReadAllTextAsync(recipePath);

        // Deserialize as Dto to filter out unnecessary data,
        // otherwise EF Core will act up with duplicate ids of same item
        var recipes = JsonSerializer.Deserialize<List<RecipeDto>>(recipeJson, opt)!;
        ctx.Recipes.AddRange(recipes.Select(dto => dto.ToEntity()));
    }
}