using System.Text.Json;
using Application.Common.Interfaces;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Seeds.Seeding.Dtos.Recipes;

namespace Persistence.LL.Seeds.Seeding;
public static class DbJsonSeeder
{
    public static async Task RunAsync(IDbContext ctx)
    {
        if (await ctx.Recipes.AnyAsync()) return;

        var itemPath = Path.Combine(AppContext.BaseDirectory, "Data", "items.json");
        var itemJson = await File.ReadAllTextAsync(itemPath);

        var items = JsonSerializer.Deserialize<List<ItemBase>>(itemJson)!;
        ctx.ItemBases.AddRange(items);

        var recipePath = Path.Combine(AppContext.BaseDirectory, "Data", "recipes.json");
        var recipeJson = await File.ReadAllTextAsync(recipePath);

        var recipes = JsonSerializer.Deserialize<List<RecipeDto>>(recipeJson)!;
        ctx.Recipes.AddRange(recipes.Select(dto => dto.ToEntity()));
        await ctx.SaveChangesAsync(CancellationToken.None);

        //logger.LogInformation("Seeded {ItemCount} items + {RecipeCount} recipes",
        //                      items.Count, recipes.Count);
    }
}