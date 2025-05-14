using System.Text.Json;
using System.Text.Json.Serialization;
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

        var opt = new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter() } };

        var itemPath = Path.Combine(AppContext.BaseDirectory, "Data", "items.json");
        var itemJson = await File.ReadAllTextAsync(itemPath);

        var items = JsonSerializer.Deserialize<List<ItemBase>>(itemJson, opt)!;
        ctx.ItemBases.AddRange(items);

        var recipePath = Path.Combine(AppContext.BaseDirectory, "Data", "recipes.json");
        var recipeJson = await File.ReadAllTextAsync(recipePath);

        // Deserialize as Dto to filter out unnecessary data,
        // otherwise EF Core will act up with duplicate ids of same item
        var recipes = JsonSerializer.Deserialize<List<RecipeDto>>(recipeJson, opt)!; 
        ctx.Recipes.AddRange(recipes.Select(dto => dto.ToEntity()));
        await ctx.SaveChangesAsync(CancellationToken.None);
    }
}