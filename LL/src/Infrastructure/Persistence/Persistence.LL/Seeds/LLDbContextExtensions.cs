using Domain.Helpers;
using Domain.Models.Achievements;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Colosseum;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.EssenceItems;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Seeds.JsonSeeding;
using Persistence.LL.Seeds.Seeding;

namespace Persistence.LL.Seeds;
public static class LLDbContextExtensions
{
    public const string CHARACTER_GUID = "11111111-1111-1111-1111-111111111111";
    private const int LOCAL_GUEST_ACCOUNT_COUNT = 96;
    private const string LOCAL_GUEST_USERNAME_PREFIX = "SeedGuest";
    private static readonly (string ItemBaseId, int Quantity)[] AdminDungeonSigils =
    [
        ("sigil_goblin_mines", 3),
        ("sigil_forgotten_catacombs", 3),
    ];
    private static readonly (Guid PlayerEssenceId, string EssenceDefinitionId)[] AdminStarterEssences =
    [
        (Guid.Parse("00000000-0000-0000-2000-000000000001"), "essence.goblin"),
        (Guid.Parse("00000000-0000-0000-2000-000000000002"), "essence.skeleton"),
        (Guid.Parse("00000000-0000-0000-2000-000000000003"), "essence.flame_imp"),
        (Guid.Parse("00000000-0000-0000-2000-000000000004"), "essence.cave_bat"),
        (Guid.Parse("00000000-0000-0000-2000-000000000005"), "essence.lumo_wisp"),
    ];
    private static readonly Guid AdminStarterEssenceLoadoutId = Guid.Parse("00000000-0000-0000-3000-000000000001");
    private const string AdminStarterEssenceLoadoutName = "Admin Starter";

    public static async Task SeedData(this LLDbContext context, IPasswordHasher<AppUser> hasher, bool seedLocalGuestAccounts = false)
    {
        // Always seed from the json files. Might update old data
        await DbJsonSeeder.RunAsync(context);
        if (await SeedAchievementAndTitleDefinitions(context))
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

        if (await SeedCreatures.EnsureAuthoredIdleRegions(context))
        {
            await context.SaveChangesAsync();
        }

#if DEBUG
        if (await SeedAdminEssenceLoadout(context))
        {
            await context.SaveChangesAsync();
        }

        if (await SeedAdminDungeonSigils(context))
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
        };

        var inventory = new Inventory()
        {
            CharacterId = character.Id,
        };

        var attributes = EntityBaseAttributeHelper.CreateEntityAttributesForLevel(
            character.Id,
            character.Level);
        await context.EntityAttributes.AddRangeAsync(attributes);

        var arenaTicketStatus = new ArenaTicketStatus()
        {
            CharacterId = character.Id,
            CurrentTickets = 5,
            LastTicketUpdate = DateTime.UtcNow,
        };
        character.ArenaTicketStatus = arenaTicketStatus;
        character.ArenaProfile = new CharacterArenaProfile { CharacterId = character.Id };

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
            var attributes = EntityBaseAttributeHelper.CreateEntityAttributesForLevel(
                character.Id,
                character.Level);
            var arenaTicketStatus = new ArenaTicketStatus
            {
                CharacterId = character.Id,
                CurrentTickets = 5,
                LastTicketUpdate = DateTime.UtcNow,
            };

            character.ArenaTicketStatus = arenaTicketStatus;
            character.ArenaProfile ??= new CharacterArenaProfile { CharacterId = character.Id };

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
        var arenaRating = Random.Shared.Next(900, 1201);
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
            ArenaProfile = new CharacterArenaProfile
            {
                CharacterId = characterId,
                Rating = arenaRating,
                LifetimeHighestRating = Math.Max(1000, arenaRating),
            },
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
                "item.essence.goblin",
                "item.essence.skeleton",
                "item.essence.flame_imp",
                "item.essence.cave_bat",
                "item.essence.lumo_wisp",
                "item.essence.goblin_warrior",
                "item.essence.goblin_archer",
                "item.essence.large_rat",
                "item.essence.frost_imp",
                "item.essence.vampire_bat",
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

    private static async Task<bool> SeedAdminDungeonSigils(LLDbContext context)
    {
        var adminCharacterId = Guid.Parse(CHARACTER_GUID);
        var hasAdminInventory = await context.Inventories
            .AnyAsync(inventory => inventory.CharacterId == adminCharacterId);

        if (!hasAdminInventory)
        {
            return false;
        }

        var sigilItemBaseIds = AdminDungeonSigils
            .Select(item => item.ItemBaseId)
            .ToArray();

        var seededItemBaseIds = await context.ItemBases
            .Where(itemBase => sigilItemBaseIds.Contains(itemBase.Id))
            .Select(itemBase => itemBase.Id)
            .ToListAsync();

        var missingItemBaseIds = sigilItemBaseIds
            .Except(seededItemBaseIds)
            .ToArray();

        if (missingItemBaseIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Admin dungeon sigil item bases were not seeded: {string.Join(", ", missingItemBaseIds)}");
        }

        var existingItems = await context.InventoryItems
            .Include(inventoryItem => inventoryItem.ItemInstance)
            .Where(inventoryItem => inventoryItem.InventoryId == adminCharacterId)
            .Where(inventoryItem => sigilItemBaseIds.Contains(inventoryItem.ItemInstance.ItemBaseId))
            .ToListAsync();

        var existingByItemBaseId = existingItems
            .GroupBy(item => item.ItemInstance.ItemBaseId)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var changed = false;
        foreach (var (itemBaseId, desiredQuantity) in AdminDungeonSigils)
        {
            if (existingByItemBaseId.TryGetValue(itemBaseId, out var matchingItems))
            {
                var currentQuantity = matchingItems.Sum(item => item.Quantity);
                if (currentQuantity < desiredQuantity)
                {
                    matchingItems[0].Quantity += desiredQuantity - currentQuantity;
                    changed = true;
                }

                continue;
            }

            var itemInstance = new ItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBaseId
            };

            await context.ItemInstances.AddAsync(itemInstance);
            await context.InventoryItems.AddAsync(new InventoryItem
            {
                InventoryId = adminCharacterId,
                ItemInstanceId = itemInstance.Id,
                Quantity = desiredQuantity
            });
            changed = true;
        }

        return changed;
    }

    private static async Task<bool> SeedAdminEssenceLoadout(LLDbContext context)
    {
        var adminCharacterId = Guid.Parse(CHARACTER_GUID);
        var hasAdminCharacter = await context.Characters
            .AnyAsync(character => character.Id == adminCharacterId);

        if (!hasAdminCharacter)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var changed = false;
        var existingEssences = await context.PlayerEssences
            .Where(essence => essence.CharacterId == adminCharacterId)
            .ToListAsync();
        var essencesByDefinitionId = existingEssences
            .ToDictionary(essence => essence.EssenceDefinitionId, StringComparer.OrdinalIgnoreCase);

        foreach (var (playerEssenceId, essenceDefinitionId) in AdminStarterEssences)
        {
            if (essencesByDefinitionId.ContainsKey(essenceDefinitionId))
            {
                continue;
            }

            var essence = new PlayerEssence
            {
                Id = playerEssenceId,
                CharacterId = adminCharacterId,
                EssenceDefinitionId = essenceDefinitionId,
                Level = 1,
                AbsorbedAt = now,
                UpdatedAt = now
            };

            await context.PlayerEssences.AddAsync(essence);
            essencesByDefinitionId[essenceDefinitionId] = essence;
            changed = true;
        }

        var loadout = await context.EssenceLoadouts
            .Include(existingLoadout => existingLoadout.Slots)
            .FirstOrDefaultAsync(existingLoadout =>
                existingLoadout.CharacterId == adminCharacterId &&
                existingLoadout.Name == AdminStarterEssenceLoadoutName);
        if (loadout is null)
        {
            loadout = new EssenceLoadout
            {
                Id = AdminStarterEssenceLoadoutId,
                CharacterId = adminCharacterId,
                Name = AdminStarterEssenceLoadoutName,
                CreatedAt = now,
                UpdatedAt = now
            };

            await context.EssenceLoadouts.AddAsync(loadout);
            changed = true;
        }
        var desiredSlots = AdminStarterEssences
            .Select((seed, slotIndex) => new
            {
                SlotIndex = slotIndex,
                PlayerEssenceId = essencesByDefinitionId[seed.EssenceDefinitionId].Id
            })
            .ToList();

        var existingSlots = loadout.Slots
            .OrderBy(slot => slot.SlotIndex)
            .ToList();
        var slotsMatch = existingSlots.Count == desiredSlots.Count &&
                         existingSlots.Zip(desiredSlots).All(pair =>
                             pair.First.SlotIndex == pair.Second.SlotIndex &&
                             pair.First.PlayerEssenceId == pair.Second.PlayerEssenceId);

        if (!slotsMatch)
        {
            context.EssenceLoadoutSlots.RemoveRange(existingSlots);
            loadout.Slots.Clear();

            var slots = desiredSlots
                .Select(slot => new EssenceLoadoutSlot
                {
                    Id = Guid.NewGuid(),
                    EssenceLoadoutId = loadout.Id,
                    SlotIndex = slot.SlotIndex,
                    PlayerEssenceId = slot.PlayerEssenceId
                })
                .ToList();

            await context.EssenceLoadoutSlots.AddRangeAsync(slots);
            loadout.UpdatedAt = now;
            changed = true;
        }

        return changed;
    }

    private static async Task<bool> SeedAchievementAndTitleDefinitions(LLDbContext context)
    {
        var changed = false;
        var catalog = AchievementTitleSeedData.Load();
        var achievementsByKey = await context.AchievementDefinitions
            .ToDictionaryAsync(x => x.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var seed in catalog.Achievements)
        {
            if (!achievementsByKey.TryGetValue(seed.Key, out var existing))
            {
                await context.AchievementDefinitions.AddAsync(Clone(seed));
                changed = true;
                continue;
            }

            changed |= Update(existing, seed);
        }

        var titlesByKey = await context.TitleDefinitions
            .ToDictionaryAsync(x => x.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var seed in catalog.Titles)
        {
            if (!titlesByKey.TryGetValue(seed.Key, out var existing))
            {
                await context.TitleDefinitions.AddAsync(Clone(seed));
                changed = true;
                continue;
            }

            changed |= Update(existing, seed);
        }

        return changed;
    }

    private static AchievementDefinition Clone(AchievementDefinition seed) => new()
    {
        Id = seed.Id,
        Key = seed.Key,
        Name = seed.Name,
        Description = seed.Description,
        Hint = seed.Hint,
        PlayerSystemMessageTemplate = seed.PlayerSystemMessageTemplate,
        GlobalSystemMessageTemplate = seed.GlobalSystemMessageTemplate,
        Category = seed.Category,
        Type = seed.Type,
        Scope = seed.Scope,
        Visibility = seed.Visibility,
        Rarity = seed.Rarity,
        Points = seed.Points,
        IsRepeatable = seed.IsRepeatable,
        IsActive = seed.IsActive,
        SortOrder = seed.SortOrder,
        IconKey = seed.IconKey,
        RequirementType = seed.RequirementType,
        RequirementTarget = seed.RequirementTarget,
        RequirementAmount = seed.RequirementAmount,
        MetadataJson = seed.MetadataJson,
        CreatedAt = seed.CreatedAt,
        UpdatedAt = seed.UpdatedAt
    };

    private static bool Update(AchievementDefinition existing, AchievementDefinition seed)
    {
        var changed = false;
        changed |= SetIfChanged(existing.Name, seed.Name, value => existing.Name = value);
        changed |= SetIfChanged(existing.Description, seed.Description, value => existing.Description = value);
        changed |= SetIfChanged(existing.Hint, seed.Hint, value => existing.Hint = value);
        changed |= SetIfChanged(
            existing.PlayerSystemMessageTemplate,
            seed.PlayerSystemMessageTemplate,
            value => existing.PlayerSystemMessageTemplate = value);
        changed |= SetIfChanged(
            existing.GlobalSystemMessageTemplate,
            seed.GlobalSystemMessageTemplate,
            value => existing.GlobalSystemMessageTemplate = value);
        changed |= SetIfChanged(existing.Category, seed.Category, value => existing.Category = value);
        changed |= SetIfChanged(existing.Type, seed.Type, value => existing.Type = value);
        changed |= SetIfChanged(existing.Scope, seed.Scope, value => existing.Scope = value);
        changed |= SetIfChanged(existing.Visibility, seed.Visibility, value => existing.Visibility = value);
        changed |= SetIfChanged(existing.Rarity, seed.Rarity, value => existing.Rarity = value);
        changed |= SetIfChanged(existing.Points, seed.Points, value => existing.Points = value);
        changed |= SetIfChanged(existing.IsRepeatable, seed.IsRepeatable, value => existing.IsRepeatable = value);
        changed |= SetIfChanged(existing.IsActive, seed.IsActive, value => existing.IsActive = value);
        changed |= SetIfChanged(existing.SortOrder, seed.SortOrder, value => existing.SortOrder = value);
        changed |= SetIfChanged(existing.IconKey, seed.IconKey, value => existing.IconKey = value);
        changed |= SetIfChanged(existing.RequirementType, seed.RequirementType, value => existing.RequirementType = value);
        changed |= SetIfChanged(existing.RequirementTarget, seed.RequirementTarget, value => existing.RequirementTarget = value);
        changed |= SetIfChanged(existing.RequirementAmount, seed.RequirementAmount, value => existing.RequirementAmount = value);
        changed |= SetIfChanged(existing.MetadataJson, seed.MetadataJson, value => existing.MetadataJson = value);

        if (changed)
        {
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return changed;
    }

    private static TitleDefinition Clone(TitleDefinition seed) => new()
    {
        Id = seed.Id,
        Key = seed.Key,
        Name = seed.Name,
        Description = seed.Description,
        Category = seed.Category,
        Rarity = seed.Rarity,
        Scope = seed.Scope,
        IsActive = seed.IsActive,
        IsHiddenUntilUnlocked = seed.IsHiddenUntilUnlocked,
        SourceAchievementKey = seed.SourceAchievementKey,
        SeasonNumber = seed.SeasonNumber,
        IconKey = seed.IconKey,
        SortOrder = seed.SortOrder,
        MetadataJson = seed.MetadataJson,
        CreatedAt = seed.CreatedAt,
        UpdatedAt = seed.UpdatedAt
    };

    private static bool Update(TitleDefinition existing, TitleDefinition seed)
    {
        var changed = false;
        changed |= SetIfChanged(existing.Name, seed.Name, value => existing.Name = value);
        changed |= SetIfChanged(existing.Description, seed.Description, value => existing.Description = value);
        changed |= SetIfChanged(existing.Category, seed.Category, value => existing.Category = value);
        changed |= SetIfChanged(existing.Rarity, seed.Rarity, value => existing.Rarity = value);
        changed |= SetIfChanged(existing.Scope, seed.Scope, value => existing.Scope = value);
        changed |= SetIfChanged(existing.IsActive, seed.IsActive, value => existing.IsActive = value);
        changed |= SetIfChanged(existing.IsHiddenUntilUnlocked, seed.IsHiddenUntilUnlocked, value => existing.IsHiddenUntilUnlocked = value);
        changed |= SetIfChanged(existing.SourceAchievementKey, seed.SourceAchievementKey, value => existing.SourceAchievementKey = value);
        changed |= SetIfChanged(existing.SeasonNumber, seed.SeasonNumber, value => existing.SeasonNumber = value);
        changed |= SetIfChanged(existing.IconKey, seed.IconKey, value => existing.IconKey = value);
        changed |= SetIfChanged(existing.SortOrder, seed.SortOrder, value => existing.SortOrder = value);
        changed |= SetIfChanged(existing.MetadataJson, seed.MetadataJson, value => existing.MetadataJson = value);

        if (changed)
        {
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return changed;
    }

    private static bool SetIfChanged<T>(T existing, T updated, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(existing, updated))
        {
            return false;
        }

        setter(updated);
        return true;
    }

}
