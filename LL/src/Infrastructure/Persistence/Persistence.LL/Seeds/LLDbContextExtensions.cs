using Domain.Helpers;
using Domain.Models.Abilities;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.LootTables;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;

namespace Persistence.LL.Seeds;
public static class LLDbContextExtensions
{
    public static async Task SeedData(this LLDbContext context, UserManager<AppUser> userManager)
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

            var attributes = EntityBaseAttributeHelper.CreateEntityAttributes(character.Id);
            context.EntityAttributes.AddRange(attributes);

            var abilities = new List<AbilityId>()
            {
                new AbilityId()
                {
                    EntityId = character.Id,
                    Id = "fireball_01"
                },
                new AbilityId()
                {
                    EntityId = character.Id,
                    Id = "heal_01"
                }
            };

            context.AbilityIds.AddRange(abilities);

            await context.SaveChangesAsync();
        }

        SeedCreatures(context);
    }


    private static void SeedCreatures(LLDbContext context)
    {
        if (!context.Creatures.Any())
        {
            // Define creature IDs
            var goblinId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var goblinWarriorId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var goblinArcherId = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var largeRatId = Guid.Parse("00000000-0000-0000-0000-000000000004");

            // Create creatures
            var creatures = new List<Creature>
            {
                new Creature { Id = goblinId, Name = "Goblin" },
                new Creature { Id = goblinWarriorId, Name = "Goblin Warrior" },
                new Creature { Id = goblinArcherId, Name = "Goblin Archer" },
                new Creature { Id = largeRatId, Name = "Large Rat" }
            };

            context.Creatures.AddRange(creatures);

            // Create attributes
            var attributes = new List<EntityAttribute>();
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(goblinId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(goblinWarriorId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(goblinArcherId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(largeRatId));

            context.EntityAttributes.AddRange(attributes);
            var abilityIds = new List<AbilityId>()
            {
                new AbilityId()
                {
                    EntityId = goblinId,
                    Id = "sneakAttack"
                },
                new AbilityId()
                {
                    EntityId = goblinId,
                    Id = "pocketDirt"
                },
                new AbilityId()
                {
                    EntityId = goblinWarriorId,
                    Id = "recklessAssault"
                },
                new AbilityId()
                {
                    EntityId = goblinWarriorId,
                    Id = "ragingCleave"
                },
                new AbilityId()
                {
                    EntityId = goblinArcherId,
                    Id = "poisonedArrows"
                },
                new AbilityId()
                {
                    EntityId = goblinArcherId,
                    Id = "snipersStrike"
                },
                new AbilityId()
                {
                    EntityId = largeRatId,
                    Id = "bigAndTough"
                },
                new AbilityId()
                {
                    EntityId = largeRatId,
                    Id = "tailWrap"
                }
            };
            context.AbilityIds.AddRange(abilityIds);


            if (!context.Regions.Any())
            {
                var areas = new List<Area>()
                {
                    new Area
                    {
                        Name = "Luno Ruins",
                        Creatures = creatures
                    }
                };

                context.Areas.AddRange(areas);

                var regions = new List<Region>
                {
                    new Region()
                    {
                        Name = "Shenic",
                        Areas = areas
                    }
                };
                context.Regions.AddRange(regions);
            }

            context.SaveChanges();
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