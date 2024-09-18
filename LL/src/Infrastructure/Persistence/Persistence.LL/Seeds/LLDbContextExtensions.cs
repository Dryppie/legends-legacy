using Domain.Models.Entities.Actors.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.LootTables;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;

namespace Persistence.LL.Seeds;
public static class LLDbContextExtensions
{
    public static async Task SeedUsersAsync(this LLDbContext context, UserManager<AppUser> userManager)
    {
        var email = "admin@hotmail.com";
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new AppUser
            {
                UserName = "admin",
                Email = email
            };

            await userManager.CreateAsync(user, "Password123!");

            var character = new Character()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
            };

            var inventory = new Inventory()
            {
                CharacterId = character.Id,
            };

            context.Characters.Add(character);
            context.Inventories.Add(inventory);

            await context.SaveChangesAsync();
        }


    }

    public static async Task SeedItemsAndLootTables(this LLDbContext context)
    {
        // Seed LootTable and Items
        if (!context.LootTables.Any() && !context.Items.Any())
        {
            // Create Items
            var sword = new Item
            {
                Id = Guid.NewGuid(),
                Name = "Sword"
            };

            var shield = new Item
            {
                Id = Guid.NewGuid(),
                Name = "Shield"
            };

            var potion = new Item
            {
                Id = Guid.NewGuid(),
                Name = "Potion"
            };

            context.Items.AddRange(sword, shield, potion);
            await context.SaveChangesAsync();

            // Create LootTable and associate items with it
            var lootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Items = new List<Item> { sword, shield, potion }
            };

            context.LootTables.Add(lootTable);
            await context.SaveChangesAsync();
        }
    }
}