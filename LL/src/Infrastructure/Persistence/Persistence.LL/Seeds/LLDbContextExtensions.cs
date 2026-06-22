using Domain.Helpers;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Colosseum;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.EssenceItems;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Seeds.Helpers;
using Persistence.LL.Seeds.JsonSeeding;
using Persistence.LL.Seeds.Seeding;

namespace Persistence.LL.Seeds;
public static class LLDbContextExtensions
{
    public const string CHARACTER_GUID = "11111111-1111-1111-1111-111111111111";
    private const int LOCAL_GUEST_ACCOUNT_COUNT = 10;
    private const string LOCAL_GUEST_USERNAME_PREFIX = "SeedGuest";
    private const string RETIRED_GOBLIN_MINES_IDLE_AREA_ID = "region_01_area_05";
    private const string RETIRED_GOBLIN_MINES_REPLACEMENT_AREA_ID = "region_01_area_06";
    private static readonly (Guid InstanceId, string ItemBaseId)[] AdminStarterTools =
    [
        (Guid.Parse("00000000-0000-0000-1000-000000000001"), "basic_pickaxe"),
        (Guid.Parse("00000000-0000-0000-1000-000000000002"), "basic_hatchet"),
        (Guid.Parse("00000000-0000-0000-1000-000000000003"), "basic_fishing_rod"),
        (Guid.Parse("00000000-0000-0000-1000-000000000004"), "basic_skinning_knife"),
    ];

    public static async Task SeedData(this LLDbContext context, IPasswordHasher<AppUser> hasher, bool seedLocalGuestAccounts = false)
    {
        // Always seed from the json files. Might update old data
        await DbJsonSeeder.RunAsync(context);
        if (await RemoveRetiredGoblinMinesIdleArea(context))
        {
            await context.SaveChangesAsync();
        }

        if (!context.Entities.Any())
        {
            await SeedCreatures.SeedCreaturesData(context);
#if DEBUG
            await SeedAdminData(context, hasher);
            await SeedInventoryItems(context);
#endif

            await context.SaveChangesAsync();
        }

        if (await SeedCreatures.EnsureRemainingRegionOneIdleAreas(context))
        {
            await context.SaveChangesAsync();
        }

#if DEBUG
        if (await SeedAdminStarterTools(context))
        {
            await context.SaveChangesAsync();
        }
#endif

        if (seedLocalGuestAccounts)
        {
            await SeedLocalGuestAccounts(context);
            await context.SaveChangesAsync();
        }
    }

    private static async Task<bool> RemoveRetiredGoblinMinesIdleArea(LLDbContext context)
    {
        var retiredArea = await context.Areas
            .Include(area => area.Creatures)
            .Include(area => area.GatheringNodes)
            .FirstOrDefaultAsync(area => area.Id == RETIRED_GOBLIN_MINES_IDLE_AREA_ID);

        if (retiredArea is null)
        {
            return false;
        }

        var replacementArea = await context.Areas
            .FirstOrDefaultAsync(area => area.Id == RETIRED_GOBLIN_MINES_REPLACEMENT_AREA_ID);

        if (replacementArea is not null)
        {
            var activeActions = await context.ActionDetails
                .OfType<CombatActionDetails>()
                .Include(details => details.Area)
                .Where(details => details.Area.Id == RETIRED_GOBLIN_MINES_IDLE_AREA_ID)
                .ToListAsync();

            foreach (var action in activeActions)
            {
                action.Area = replacementArea;
            }
        }

        context.Areas.Remove(retiredArea);
        return true;
    }

    private static async Task SeedAdminData(LLDbContext context, IPasswordHasher<AppUser> hasher)
    {
        var email = "admin@hotmail.com";
        var user = new AppUser
        {
            Username = "admin",
            Email = email,
            PasswordHash = hasher.HashPassword(null!, "Password123!"),
            EmailConfirmed = true,
            IsGuest = false,
        };

        await context.Users.AddAsync(user);

        var character = new Character()
        {
            Id = Guid.Parse(CHARACTER_GUID),
            UserId = user.Id,
            Name = "admin",
            ImagePath = "player",
            Level = 50,
            Cinders = 5719,
            Soulstones = 5000,
            Professions = ProfessionsSeederHelper.CreateProfessions(Guid.Parse(CHARACTER_GUID)),
        };

        var inventory = new Inventory()
        {
            CharacterId = character.Id,
        };

        var attributes = EntityBaseAttributeHelper.CreateEntityAttributes(character.Id);
        await context.EntityAttributes.AddRangeAsync(attributes);

        var arenaTicketStatus = new ArenaTicketStatus()
        {
            CharacterId = character.Id,
            CurrentTickets = 5,
            LastTicketUpdate = DateTime.UtcNow,
        };
        character.ArenaTicketStatus = arenaTicketStatus;

        var equipmentSlots = SeedEquipmentSlots(character);
        context.EquipmentSlots.AddRange(equipmentSlots);
        context.Characters.Add(character);
        context.Inventories.Add(inventory);
    }

    private static async Task SeedLocalGuestAccounts(LLDbContext context)
    {
        var existingSeedGuestCount = await context.Users.CountAsync(user =>
            user.IsGuest && user.Username.StartsWith(LOCAL_GUEST_USERNAME_PREFIX));
        var guestsToCreate = LOCAL_GUEST_ACCOUNT_COUNT - existingSeedGuestCount;

        if (guestsToCreate <= 0)
        {
            return;
        }

        var usernames = await context.Users
            .Select(user => user.Username)
            .ToListAsync();
        var usedUsernames = new HashSet<string>(usernames, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < guestsToCreate; i++)
        {
            var user = AppUser.Guest();
            user.Username = GenerateLocalGuestName(usedUsernames);

            var character = CreateLocalGuestCharacter(user);
            var inventory = new Inventory
            {
                CharacterId = character.Id,
            };
            var attributes = EntityBaseAttributeHelper.CreateEntityAttributes(character.Id);
            var arenaTicketStatus = new ArenaTicketStatus
            {
                CharacterId = character.Id,
                CurrentTickets = 5,
                LastTicketUpdate = DateTime.UtcNow,
            };

            character.ArenaTicketStatus = arenaTicketStatus;

            await context.Users.AddAsync(user);
            context.Characters.Add(character);
            context.Inventories.Add(inventory);
            context.EntityAttributes.AddRange(attributes);
            context.EquipmentSlots.AddRange(SeedEquipmentSlots(character));
        }
    }

    private static Character CreateLocalGuestCharacter(AppUser user)
    {
        var characterId = Guid.NewGuid();
        return new Character
        {
            Id = characterId,
            UserId = user.Id,
            User = user,
            Name = user.Username,
            ImagePath = "player",
            Level = Random.Shared.Next(1, 16),
            Cinders = Random.Shared.Next(250, 5001),
            Soulstones = Random.Shared.Next(0, 501),
            ArenaRating = Random.Shared.Next(900, 1201),
            Professions = ProfessionsSeederHelper.CreateProfessions(characterId),
        };
    }

    private static string GenerateLocalGuestName(ISet<string> usedUsernames)
    {
        var prefixes = new[]
        {
            "Ashen", "Bright", "Clever", "Daring", "Duskworn",
            "Ember", "Iron", "Lucky", "Noble", "Silent",
            "Steady", "Storm", "Swift", "Vivid", "Wild",
        };
        var titles = new[]
        {
            "Alchemist", "Archivist", "Champion", "Delver", "Keeper",
            "Pathfinder", "Pilgrim", "Ranger", "Scholar", "Sentinel",
            "Strider", "Scribe", "Seeker", "Voyager", "Warden",
        };

        string username;
        do
        {
            username =
                $"{LOCAL_GUEST_USERNAME_PREFIX}_{prefixes[Random.Shared.Next(prefixes.Length)]}{titles[Random.Shared.Next(titles.Length)]}_{Random.Shared.Next(1000, 10000)}";
        } while (!usedUsernames.Add(username));

        return username;
    }

    private static List<EquipmentSlot> SeedEquipmentSlots(Entity entity)
    {

        var slotTypes = Enum.GetValues(typeof(EquipmentSlotType)).Cast<EquipmentSlotType>();

        // Create an equipment slot for each enum value
        var equipmentSlots = slotTypes
            .Select(type => new EquipmentSlot
            {
                EntityId = entity.Id,
                EquipmentSlotType = type
            })
            .ToList();

        return equipmentSlots;
    }

    public static async Task SeedInventoryItems(LLDbContext context)
    {
        if (!context.InventoryItems.Any())
        {
            var essenceItemBaseIds = new[]
            {
                "item.essence.goblin_ambusher",
                "item.essence.skeleton_guardian",
                "item.essence.fire_ant",
                "item.essence.cave_bat",
                "item.essence.necroshade_wraith",
                "item.essence.legacy.goblin",
                "item.essence.legacy.goblin_warrior",
                "item.essence.legacy.goblin_archer",
                "item.essence.legacy.large_rat",
                "item.essence.legacy.flame_imp",
            };

            var essenceItemInstances = essenceItemBaseIds
                .Select((itemBaseId, index) => new EssenceItemInstance
                {
                    Id = Guid.Parse($"00000000-0000-0000-0000-{index + 1:000000000000}"),
                    ItemBaseId = itemBaseId,
                })
                .ToList();

            var inventoryItems = essenceItemInstances
                .Select(instance => new InventoryItem
                {
                    InventoryId = Guid.Parse(CHARACTER_GUID),
                    ItemInstanceId = instance.Id,
                    Quantity = 1
                })
                .ToList();

            await context.ItemInstances.AddRangeAsync(essenceItemInstances);
            await context.InventoryItems.AddRangeAsync(inventoryItems);
        }
    }

    private static async Task<bool> SeedAdminStarterTools(LLDbContext context)
    {
        var adminCharacterId = Guid.Parse(CHARACTER_GUID);
        var hasAdminInventory = await context.Inventories
            .AnyAsync(inventory => inventory.CharacterId == adminCharacterId);

        if (!hasAdminInventory)
        {
            return false;
        }

        var starterToolItemBaseIds = AdminStarterTools
            .Select(tool => tool.ItemBaseId)
            .ToArray();

        var seededItemBaseIds = await context.ItemBases
            .Where(itemBase => starterToolItemBaseIds.Contains(itemBase.Id))
            .Select(itemBase => itemBase.Id)
            .ToListAsync();

        var missingItemBaseIds = starterToolItemBaseIds
            .Except(seededItemBaseIds)
            .ToArray();

        if (missingItemBaseIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Admin starter tool item bases were not seeded: {string.Join(", ", missingItemBaseIds)}");
        }

        var existingStarterToolItemBaseIds = await context.InventoryItems
            .Where(inventoryItem => inventoryItem.InventoryId == adminCharacterId)
            .Where(inventoryItem => starterToolItemBaseIds.Contains(inventoryItem.ItemInstance.ItemBaseId))
            .Select(inventoryItem => inventoryItem.ItemInstance.ItemBaseId)
            .ToListAsync();

        var existingStarterToolItemBaseIdSet = existingStarterToolItemBaseIds.ToHashSet();
        var missingTools = AdminStarterTools
            .Where(tool => !existingStarterToolItemBaseIdSet.Contains(tool.ItemBaseId))
            .ToArray();

        if (missingTools.Length == 0)
        {
            return false;
        }

        var missingToolInstanceIds = missingTools
            .Select(tool => tool.InstanceId)
            .ToArray();

        var existingInstanceIds = await context.ItemInstances
            .Where(instance => missingToolInstanceIds.Contains(instance.Id))
            .Select(instance => instance.Id)
            .ToListAsync();

        var existingInstanceIdSet = existingInstanceIds.ToHashSet();
        var itemInstances = missingTools
            .Where(tool => !existingInstanceIdSet.Contains(tool.InstanceId))
            .Select(tool => new EquipmentInstance
            {
                Id = tool.InstanceId,
                ItemBaseId = tool.ItemBaseId
            })
            .ToList();

        var inventoryItems = missingTools
            .Select(tool => new InventoryItem
            {
                InventoryId = adminCharacterId,
                ItemInstanceId = tool.InstanceId,
                Quantity = 1
            })
            .ToList();

        await context.ItemInstances.AddRangeAsync(itemInstances);
        await context.InventoryItems.AddRangeAsync(inventoryItems);

        return true;
    }

}
