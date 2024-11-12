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
            var goblinShamanId = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var largeRatId = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var flameImpId = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var frostImpId = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var shadowImpId = Guid.Parse("00000000-0000-0000-0000-000000000008");
            var greenSlimeId = Guid.Parse("00000000-0000-0000-0000-000000000009");
            var redSlimeId = Guid.Parse("00000000-0000-0000-0000-00000000000A");
            var blueSlimeId = Guid.Parse("00000000-0000-0000-0000-00000000000B");
            var brownSlimeId = Guid.Parse("00000000-0000-0000-0000-00000000000C");
            var transparentSlimeId = Guid.Parse("00000000-0000-0000-0000-00000000000D");
            var rainbowSlimeId = Guid.Parse("00000000-0000-0000-0000-00000000000E");
            var caveBatId = Guid.Parse("00000000-0000-0000-0000-00000000000F");
            var giantBatId = Guid.Parse("00000000-0000-0000-0000-000000000010");
            var vampireBatId = Guid.Parse("00000000-0000-0000-0000-000000000011");
            var viperId = Guid.Parse("00000000-0000-0000-0000-000000000012");
            var constrictorSnakeId = Guid.Parse("00000000-0000-0000-0000-000000000013");
            var venomousSnakeId = Guid.Parse("00000000-0000-0000-0000-000000000014");
            var skeletonId = Guid.Parse("00000000-0000-0000-0000-000000000015");
            var spiderId = Guid.Parse("00000000-0000-0000-0000-000000000016");
            var venomousSpiderlingId = Guid.Parse("00000000-0000-0000-0000-000000000017");
            var giantSpiderId = Guid.Parse("00000000-0000-0000-0000-000000000018");
            var antSoldierId = Guid.Parse("00000000-0000-0000-0000-000000000019");
            var fireAntId = Guid.Parse("00000000-0000-0000-0000-00000000001A");
            var woodNymphId = Guid.Parse("00000000-0000-0000-0000-00000000001B");
            var forestSpiritId = Guid.Parse("00000000-0000-0000-0000-00000000001C");
            var pixieId = Guid.Parse("00000000-0000-0000-0000-00000000001D");
            var illusionFoxId = Guid.Parse("00000000-0000-0000-0000-00000000001E");
            var enchantedFairyId = Guid.Parse("00000000-0000-0000-0000-00000000001F");
            var nightshadeBlossomId = Guid.Parse("00000000-0000-0000-0000-000000000020");
            var gladePantherId = Guid.Parse("00000000-0000-0000-0000-000000000021");
            var treantSaplingId = Guid.Parse("00000000-0000-0000-0000-000000000022");
            // Create creatures
            var creatures = new List<Creature>
            {
                new Creature { Id = goblinId, Name = "Goblin" },
                new Creature { Id = goblinWarriorId, Name = "Goblin Warrior" },
                new Creature { Id = goblinArcherId, Name = "Goblin Archer" },
                new Creature { Id = goblinShamanId, Name = "Goblin Shaman" },
                new Creature { Id = largeRatId, Name = "Large Rat" },
                new Creature { Id = flameImpId, Name = "Flame Imp" },
                new Creature { Id = frostImpId, Name = "Frost Imp" },
                new Creature { Id = shadowImpId, Name = "Shadow Imp" },
                new Creature { Id = greenSlimeId, Name = "Green Slime" },
                new Creature { Id = redSlimeId, Name = "Red Slime" },
                new Creature { Id = blueSlimeId, Name = "Blue Slime" },
                new Creature { Id = brownSlimeId, Name = "Brown Slime" },
                new Creature { Id = transparentSlimeId, Name = "Transparent Slime" },
                new Creature { Id = rainbowSlimeId, Name = "Rainbow Slime" },
                new Creature { Id = caveBatId, Name = "Cave Bat" },
                new Creature { Id = giantBatId, Name = "Giant Bat" },
                new Creature { Id = vampireBatId, Name = "Vampire Bat" },
                new Creature { Id = viperId, Name = "Viper" },
                new Creature { Id = constrictorSnakeId, Name = "Constrictor Snake" },
                new Creature { Id = venomousSnakeId, Name = "Venomous Snake" },
                new Creature { Id = skeletonId, Name = "Skeleton" },
                new Creature { Id = spiderId, Name = "Spider" },
                new Creature { Id = venomousSpiderlingId, Name = "Venomous Spiderling" },
                new Creature { Id = giantSpiderId, Name = "Giant Spider" },
                new Creature { Id = antSoldierId, Name = "Ant Soldier" },
                new Creature { Id = fireAntId, Name = "Fire Ant" },
                new Creature { Id = woodNymphId, Name = "Wood Nymph" },
                new Creature { Id = forestSpiritId, Name = "Forest Spirit" },
                new Creature { Id = pixieId, Name = "Pixie" },
                new Creature { Id = illusionFoxId, Name = "Illusion Fox" },
                new Creature { Id = enchantedFairyId, Name = "Enchanted Fairy" },
                new Creature { Id = nightshadeBlossomId, Name = "Nightshade Blossom" },
                new Creature { Id = gladePantherId, Name = "Glade Panther" },
                new Creature { Id = treantSaplingId, Name = "Treant Sapling" }
            };

            context.Creatures.AddRange(creatures);

            // Create attributes
            var attributes = new List<EntityAttribute>();

            foreach (var creature in creatures)
            {
                attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(creature.Id));
            }

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

            var treeLog = new Item
            {
                Id = Guid.NewGuid(),
                Name = "Tree Log"
            };

            var oakLog = new Item
            {
                Id = Guid.NewGuid(),
                Name = "Oak Log"
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