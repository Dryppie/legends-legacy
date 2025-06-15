using Domain.Helpers;
using Domain.Models.Colosseum;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.EssenceItems;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;
using Persistence.LL.Seeds.Helpers;
using Persistence.LL.Seeds.JsonSeeding;
using Persistence.LL.Seeds.Seeding;

namespace Persistence.LL.Seeds;
public static class LLDbContextExtensions
{
    public const string CHARACTER_GUID = "11111111-1111-1111-1111-111111111111";

    public static async Task SeedData(this LLDbContext context, IPasswordHasher<AppUser> hasher)
    {
        if (!context.Entities.Any())
        {
            await DbJsonSeeder.RunAsync(context);
            await SeedCreatures.SeedCreaturesData(context);
            await SeedItems.SeedItemsData(context);
            await SeedProfessions.SeedProfessionsData(context);
#if DEBUG
            await SeedAdminData(context, hasher);
#endif
            await SeedInventoryItems(context);

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
            Level = 1,
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

        var essences = new List<Essence>()
        {
            new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Goblin's Essence",
                ActiveAbilityId = "sneakAttack",
                PassiveAbilityId = "pocketDirt"
            },
            new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Goblin Warrior's Essence",
                ActiveAbilityId = "ragingCleave",
                PassiveAbilityId = "recklessAssault"
            }
        };

        var essenceSlots = new List<EssenceSlot>()
        {
            new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = essences.First(),
                EntityId = character.Id,
            },
            new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = essences.Last(),
                EntityId = character.Id,
            },
            new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                EntityId = character.Id,
            },
            new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                EntityId = character.Id,
            },new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                EntityId = character.Id,
            },
            new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                EntityId = character.Id,
            },
        };

        var arenaTicketStatus = new ArenaTicketStatus()
        {
            CharacterId = character.Id,
            CurrentTickets = 5,
            LastTicketUpdate = DateTime.UtcNow,
        };
        character.ArenaTicketStatus = arenaTicketStatus;

        var equipmentSlots = SeedEquipmentSlots(character);
        context.EquipmentSlots.AddRange(equipmentSlots);
        character.EssenceSlots = essenceSlots;
        context.Characters.Add(character);
        context.Inventories.Add(inventory);
        await context.Essences.AddRangeAsync(essences);
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
            var goblinEssenceItemInstance = new EssenceItemInstance
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                ItemBaseId = "goblinId",
            };
            var ratEssenceItemInstance = new EssenceItemInstance
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                ItemBaseId = "largeRatId",
            };
            var inventoryItemGoblinEssence = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemInstanceId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // Copied directly from GoblinEssenceItem. Same ID
                Quantity = 1
            };

            var inventoryItemRatEssence = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemInstanceId = Guid.Parse("00000000-0000-0000-0000-000000000004"), // Copied directly from LargeRatEssenceItem. Same ID
                Quantity = 1
            };

            await context.ItemInstances.AddRangeAsync(goblinEssenceItemInstance, ratEssenceItemInstance);
            await context.InventoryItems.AddRangeAsync(inventoryItemGoblinEssence, inventoryItemRatEssence);
        }
    }

}